using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodStreetGuide.Models;
using FoodStreetGuide.Database;

namespace FoodStreetGuide.Services
{
    /// <summary>
    /// Manages offline functionality: caching, sync queue, connectivity check
    /// </summary>
    public class OfflineManager
    {
        private readonly DatabaseService _databaseService;
        private readonly HttpClient _httpClient;
        private bool _isOnline = true;
        private DateTime _lastSyncTime = DateTime.MinValue;
        
        // Queue for offline analytics
        private readonly List<PendingAnalytics> _pendingAnalytics = new();
        private readonly object _queueLock = new object();
        
        public OfflineManager(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }
        
        /// <summary>
        /// Check if device is online
        /// </summary>
        public async Task<bool> IsOnlineAsync()
        {
            try
            {
                // Try to reach the API server
                var response = await _httpClient.GetAsync("http://10.0.2.2/foodtour-admin/api/get-pois.php");
                var wasOffline = !_isOnline;
                _isOnline = response.IsSuccessStatusCode;
                
                if (wasOffline && _isOnline)
                {
                    System.Diagnostics.Debug.WriteLine("[OfflineManager] Back online! Syncing pending data...");
                    _ = Task.Run(SyncPendingDataAsync);
                }
                
                return _isOnline;
            }
            catch
            {
                _isOnline = false;
                return false;
            }
        }
        
        /// <summary>
        /// Get POIs - from API if online, from cache if offline
        /// </summary>
        public async Task<List<POI>> GetPOIsAsync(ApiService apiService)
        {
            var isOnline = await IsOnlineAsync();
            
            if (isOnline)
            {
                try
                {
                    // Try to sync from API
                    var result = await apiService.GetPOIsAsync();
                    if (result.Success && result.Data?.Data != null)
                    {
                        // Update local database
                        await SyncPOIsToDatabaseAsync(result.Data.Data);
                        _lastSyncTime = DateTime.Now;
                        
                        System.Diagnostics.Debug.WriteLine($"[OfflineManager] Synced {result.Data.Data.Length} POIs from API");
                        return await _databaseService.GetPOIsAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OfflineManager] API sync failed: {ex.Message}");
                }
            }
            
            // Fall back to cached data
            var cachedPOIs = await _databaseService.GetPOIsAsync();
            System.Diagnostics.Debug.WriteLine($"[OfflineManager] Using {cachedPOIs.Count} cached POIs (Last sync: {_lastSyncTime})");
            return cachedPOIs;
        }
        
        /// <summary>
        /// Queue analytics event for later sync
        /// </summary>
        public void QueueAnalytics(string type, string deviceId, int? poiId = null, string? sessionId = null)
        {
            lock (_queueLock)
            {
                _pendingAnalytics.Add(new PendingAnalytics
                {
                    Type = type,
                    DeviceId = deviceId,
                    POIId = poiId,
                    SessionId = sessionId,
                    Timestamp = DateTime.Now,
                    RetryCount = 0
                });
                
                System.Diagnostics.Debug.WriteLine($"[OfflineManager] Queued {type} analytics (total pending: {_pendingAnalytics.Count})");
            }
            
            // Try to sync immediately if online
            _ = Task.Run(SyncPendingDataAsync);
        }
        
        /// <summary>
        /// Sync all pending data when back online
        /// </summary>
        public async Task SyncPendingDataAsync()
        {
            var isOnline = await IsOnlineAsync();
            if (!isOnline) return;
            
            List<PendingAnalytics> toSync;
            lock (_queueLock)
            {
                toSync = _pendingAnalytics.Where(a => a.RetryCount < 3).ToList();
            }
            
            if (toSync.Count == 0) return;
            
            var apiService = new ApiService();
            var successCount = 0;
            
            foreach (var analytics in toSync)
            {
                try
                {
                    var result = await apiService.PostAnalyticsAsync(analytics.Type, analytics.DeviceId, analytics.POIId, analytics.SessionId);
                    if (result.Success)
                    {
                        lock (_queueLock)
                        {
                            _pendingAnalytics.Remove(analytics);
                        }
                        successCount++;
                    }
                    else
                    {
                        analytics.RetryCount++;
                    }
                }
                catch
                {
                    analytics.RetryCount++;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[OfflineManager] Synced {successCount}/{toSync.Count} pending analytics");
        }
        
        /// <summary>
        /// Sync POIs to local database
        /// </summary>
        private async Task SyncPOIsToDatabaseAsync(POIApiData[] poiItems)
        {
            var existingPOIs = await _databaseService.GetPOIsAsync();
            var existingIds = existingPOIs.Select(p => p.Id).ToHashSet();
            
            int added = 0, updated = 0;
            
            foreach (var poiData in poiItems)
            {
                var poi = new POI
                {
                    Id = poiData.Id,
                    NameVi = poiData.NameVi ?? "",
                    NameEn = poiData.NameEn ?? "",
                    DescriptionVi = poiData.DescriptionVi ?? "",
                    DescriptionEn = poiData.DescriptionEn ?? "",
                    Address = poiData.Address ?? "",
                    Phone = poiData.Phone ?? "",
                    OpeningHours = poiData.OpeningHours ?? "",
                    ImageUrl = poiData.ImageUrl,
                    VisitCount = poiData.VisitCount,
                    CheckInCount = poiData.CheckInCount,
                    Rating = poiData.Rating,
                    Latitude = poiData.Latitude ?? 0,
                    Longitude = poiData.Longitude ?? 0,
                    Radius = poiData.Radius > 0 ? poiData.Radius : 100,  // Default 100m
                    Priority = poiData.Priority > 0 ? poiData.Priority : 1, // Default priority 1
                    Status = poiData.Status == "approved" ? "active" : poiData.Status,
                    LastUpdated = DateTime.Now
                };
                
                if (existingIds.Contains(poi.Id))
                {
                    // Update existing
                    await _databaseService.UpdatePOIAsync(poi);
                    updated++;
                }
                else
                {
                    // Insert new
                    await _databaseService.SavePOIAsync(poi);
                    added++;
                }
            }
            
            // Mark deleted POIs (not in API response but in local DB)
            var apiIds = poiItems.Select(p => p.Id).ToHashSet();
            var deletedPOIs = existingPOIs.Where(p => !apiIds.Contains(p.Id) && p.Status == "active").ToList();
            foreach (var deleted in deletedPOIs)
            {
                deleted.Status = "deleted";
                await _databaseService.UpdatePOIAsync(deleted);
            }
            
            System.Diagnostics.Debug.WriteLine($"[OfflineManager] Database updated: {added} added, {updated} updated, {deletedPOIs.Count} marked deleted");
        }
        
        /// <summary>
        /// Get sync status for UI display
        /// </summary>
        public string GetSyncStatus()
        {
            var pendingCount = _pendingAnalytics.Count;
            var lastSync = _lastSyncTime == DateTime.MinValue ? "Chưa đồng bộ" : _lastSyncTime.ToString("HH:mm:ss");
            var onlineStatus = _isOnline ? "🟢 Online" : "🔴 Offline";
            
            return $"{onlineStatus} | Đồng bộ: {lastSync} | Chờ gửi: {pendingCount}";
        }
        
        private class PendingAnalytics
        {
            public string Type { get; set; } = "";
            public string DeviceId { get; set; } = "";
            public int? POIId { get; set; }
            public string? SessionId { get; set; }
            public DateTime Timestamp { get; set; }
            public int RetryCount { get; set; }
        }
    }
    
    /// <summary>
    /// Extension methods for POI
    /// </summary>
    public static class POIExtensions
    {
        public static DateTime LastUpdated { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public interface IWebAdminService
{
    Task<List<POI>> GetAllPOIsAsync();
    Task<POI> GetPOIAsync(int id);
    Task<POI> CreatePOIAsync(POI poi);
    Task<POI> UpdatePOIAsync(int id, POI poi);
    Task<bool> DeletePOIAsync(int id);
    Task SyncFromWebAdminAsync();
    Task SyncToWebAdminAsync(); // Push local changes to web
    Task FullSyncAsync(); // Two-way sync
    Task<bool> TestConnectionAsync();
    string GetApiUrl();
    void SetApiUrl(string url);
}

public class WebAdminService : IWebAdminService
{
    private readonly HttpClient _httpClient;
    private readonly IDatabaseService _databaseService;
    // Default URL for Android emulator (10.0.2.2 = host localhost)
    // XAMPP: http://localhost/FoodStreetGuide.Admin/api
    // Android: http://10.0.2.2/FoodStreetGuide.Admin/api
    private string _apiBaseUrl = "http://10.0.2.2/FoodStreetGuide.Admin/api";
    private const string API_KEY = "foodstreet_mobile_2024";

    public string GetApiUrl() => _apiBaseUrl;
    public void SetApiUrl(string url) => _apiBaseUrl = url;

    public WebAdminService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        
        // Add default headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/pois.php?api_key={API_KEY}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Connection test failed: {ex.Message}");
            return false;
        }
    }

    public async Task<List<POI>> GetAllPOIsAsync()
    {
        try
        {
            var url = $"{_apiBaseUrl}/pois.php?api_key={API_KEY}";
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Fetching from: {url}");
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<POI>>>();
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Fetched {result?.Data?.Count ?? 0} POIs");
            return result?.Data ?? new List<POI>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Error fetching POIs: {ex.Message}");
            return new List<POI>();
        }
    }

    public async Task<POI> GetPOIAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/pois.php?id={id}&api_key={API_KEY}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<POI>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Error fetching POI {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<POI> CreatePOIAsync(POI poi)
    {
        try
        {
            var content = JsonContent.Create(poi);
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/pois.php?api_key={API_KEY}", content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<POI>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Error creating POI: {ex.Message}");
            return null;
        }
    }

    public async Task<POI> UpdatePOIAsync(int id, POI poi)
    {
        try
        {
            var content = JsonContent.Create(poi);
            var request = new HttpRequestMessage(HttpMethod.Put, $"{_apiBaseUrl}/pois.php?api_key={API_KEY}")
            {
                Content = content
            };
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<POI>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Error updating POI {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeletePOIAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/pois.php?id={id}&api_key={API_KEY}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Error deleting POI {id}: {ex.Message}");
            return false;
        }
    }

    public async Task SyncFromWebAdminAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[WebAdminService] Starting sync FROM web admin...");
            var remotePOIs = await GetAllPOIsAsync();
            int synced = 0, added = 0, updated = 0;
            
            foreach (var poi in remotePOIs)
            {
                var existing = await _databaseService.GetPOIsAsync();
                var found = existing.FirstOrDefault(p => p.Id == poi.Id);
                
                if (found == null)
                {
                    await _databaseService.AddPOIAsync(poi);
                    added++;
                }
                else
                {
                    // Check if remote is newer
                    var remoteUpdated = poi.GetType().GetProperty("UpdatedAt")?.GetValue(poi) as string;
                    var localUpdated = found.GetType().GetProperty("UpdatedAt")?.GetValue(found) as string;
                    
                    await _databaseService.UpdatePOIAsync(poi);
                    updated++;
                }
                synced++;
            }
            
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Synced {synced} POIs (added: {added}, updated: {updated})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Sync FROM error: {ex.Message}");
        }
    }

    public async Task SyncToWebAdminAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[WebAdminService] Starting sync TO web admin...");
            var localPOIs = await _databaseService.GetPOIsAsync();
            int synced = 0;
            
            foreach (var poi in localPOIs)
            {
                // Try to update, if fails (not exists), create
                var updated = await UpdatePOIAsync(poi.Id, poi);
                if (updated == null)
                {
                    await CreatePOIAsync(poi);
                }
                synced++;
            }
            
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Pushed {synced} POIs to web admin");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebAdminService] Sync TO error: {ex.Message}");
        }
    }

    public async Task FullSyncAsync()
    {
        System.Diagnostics.Debug.WriteLine("[WebAdminService] Starting FULL two-way sync...");
        
        // First push local changes
        await SyncToWebAdminAsync();
        
        // Then pull remote changes
        await SyncFromWebAdminAsync();
        
        System.Diagnostics.Debug.WriteLine("[WebAdminService] Full sync completed");
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Error { get; set; }
}

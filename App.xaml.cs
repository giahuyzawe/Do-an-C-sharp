using FoodStreetGuide.Services;
using FoodStreetGuide.Models;
using FoodStreetGuide.Database;
using System.Timers;

namespace FoodStreetGuide;

public partial class App : Application
{
    public static DatabaseService Database { get; private set; }
    private string _currentDeviceId = string.Empty;
    private string _currentSessionId = string.Empty;
    private const int SessionTimeoutMinutes = 30;

    public App(DatabaseService databaseService)
    {
        InitializeComponent();

        Database = databaseService;

        //Database.Init();

        // Initialize analytics tracking
        _ = Task.Run(async () =>
        {
            try
            {
                await databaseService.Init();
                
                // Get or create UNIQUE DeviceId per device (NOT shared via backup)
                // Use combination of install timestamp + random to ensure uniqueness
                var installKey = "AppInstallId";
                var installId = Preferences.Get(installKey, string.Empty);
                if (string.IsNullOrEmpty(installId))
                {
                    // First time install - create unique install ID
                    installId = Guid.NewGuid().ToString("N")[..12];
                    Preferences.Set(installKey, installId);
                }
                
                // DeviceId = InstallId (unique per device installation)
                _currentDeviceId = installId;
                
                // Also save to legacy key for compatibility
                Preferences.Set("DeviceId", _currentDeviceId);
                
                // Record app visit (local) - will be used for session tracking
                await databaseService.RecordAppVisitAsync(_currentDeviceId, DateTime.Now, AppInfo.Version.ToString(), DeviceInfo.Platform.ToString());
                var deviceIdShort = _currentDeviceId?.Length >= 8 ? _currentDeviceId.Substring(0, 8) : _currentDeviceId ?? "unknown";
                System.Diagnostics.Debug.WriteLine($"[App] Visit recorded for device: {deviceIdShort}...");
                
                // Initialize OfflineManager for sync and offline support
                var offlineManager = new OfflineManager(databaseService);
                
                // Sync POIs from Web Admin API with offline support
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiService = new ApiService();
                        
                        // Check connectivity
                        var isOnline = await offlineManager.IsOnlineAsync();
                        System.Diagnostics.Debug.WriteLine($"[App] Network status: {(isOnline ? "Online" : "Offline")}");
                        
                        // Get POIs (from API if online, from cache if offline)
                        var pois = await offlineManager.GetPOIsAsync(apiService);
                        System.Diagnostics.Debug.WriteLine($"[App] Total POIs available: {pois.Count}");
                        
                        if (pois.Count > 0)
                        {
                            // Update geofence with POIs
                            var geofenceEngine = ServiceProviderHelper.GetService<IGeofenceEngine>();
                            if (geofenceEngine != null)
                            {
                                geofenceEngine.SetPOIs(pois);
                                System.Diagnostics.Debug.WriteLine($"[App] Geofence updated with {pois.Count} POIs");
                            }
                            
                            // Save sync status
                            Preferences.Set("LastPOISync", DateTime.Now.ToString("O"));
                            Preferences.Set("POICount", pois.Count);
                        }
                        else if (!isOnline)
                        {
                            System.Diagnostics.Debug.WriteLine("[App] Offline and no cached POIs - showing offline mode");
                            // Could show offline banner here
                        }
                    }
                    catch (Exception poiEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App] POI sync error: {poiEx.Message}");
                        // Still try to load from cache
                        var cachedPOIs = await databaseService.GetPOIsAsync();
                        if (cachedPOIs.Count > 0)
                        {
                            var geofenceEngine = ServiceProviderHelper.GetService<IGeofenceEngine>();
                            geofenceEngine?.SetPOIs(cachedPOIs);
                            System.Diagnostics.Debug.WriteLine($"[App] Loaded {cachedPOIs.Count} POIs from cache after error");
                        }
                    }
                });
                
                // Sync SQLite data to Web Admin (reviews, check-ins, saved POIs)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(10000); // Wait 10s after POI sync
                        await SyncLocalDataToWebAdminAsync();
                    }
                    catch (Exception syncEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App] Data sync error: {syncEx.Message}");
                    }
                });
                
                // Start new session or continue existing
                await StartOrContinueSessionAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Failed to record visit: {ex.Message}");
            }
        });

        MainPage = new AppShell();
    }

    private async Task StartOrContinueSessionAsync()
    {
        if (string.IsNullOrEmpty(_currentDeviceId)) return;

        // Check last activity
        var lastActivity = await Database.GetLastActivityAsync(_currentDeviceId);
        
        if (lastActivity != null && (DateTime.Now - lastActivity.Value) < TimeSpan.FromMinutes(SessionTimeoutMinutes))
        {
            // Continue existing session - get current session ID
            _currentSessionId = Preferences.Get("CurrentSessionId", string.Empty);
            if (!string.IsNullOrEmpty(_currentSessionId))
            {
                await Database.RecordAppOpenAsync(_currentDeviceId, _currentSessionId);
                var sessionIdShort = _currentSessionId?.Length >= 8 ? _currentSessionId.Substring(0, 8) : _currentSessionId ?? "unknown";
                System.Diagnostics.Debug.WriteLine($"[App] App open recorded in existing session: {sessionIdShort}...");
                // Start heartbeat for online tracking even for existing sessions
                StartHeartbeatTracking();
                return;
            }
        }

        // Start new session - only send analytics on NEW session (app kill & reopen)
        _currentSessionId = await Database.StartNewSessionAsync(_currentDeviceId);
        Preferences.Set("CurrentSessionId", _currentSessionId);
        var newSessionIdShort = _currentSessionId?.Length >= 8 ? _currentSessionId.Substring(0, 8) : _currentSessionId ?? "unknown";
        System.Diagnostics.Debug.WriteLine($"[App] New session started: {newSessionIdShort}...");
        
        // Send app_visit analytics only on new session
        _ = Task.Run(async () =>
        {
            try
            {
                var apiService = new ApiService();
                var result = await apiService.PostAnalyticsAsync("app_visit", _currentDeviceId);
                if (result.Success)
                {
                    System.Diagnostics.Debug.WriteLine("[App] Analytics sent to Web Admin (new session)");
                }
            }
            catch (Exception apiEx)
            {
                System.Diagnostics.Debug.WriteLine($"[App] API error: {apiEx.Message}");
            }
        });
        
        // Start heartbeat to track ONLINE status (every 30 seconds)
        StartHeartbeatTracking();
    }
    
    private System.Timers.Timer _heartbeatTimer;
    private bool _isDisposed = false;
    
    ~App()
    {
        // Mark as disposed to stop timer callbacks
        _isDisposed = true;
        
        // Cleanup timer when App is destroyed
        if (_heartbeatTimer != null)
        {
            _heartbeatTimer.Stop();
            _heartbeatTimer.Dispose();
            _heartbeatTimer = null;
        }
    }
    
    private void StartHeartbeatTracking()
    {
        // Stop existing timer if running (prevent multiple instances)
        if (_heartbeatTimer != null)
        {
            _heartbeatTimer.Stop();
            _heartbeatTimer.Dispose();
            _heartbeatTimer = null;
        }
        
        _heartbeatTimer = new System.Timers.Timer(30000); // 30 seconds
        _heartbeatTimer.Elapsed += async (s, e) =>
        {
            // Skip if App is being disposed
            if (_isDisposed) return;
            
            if (!string.IsNullOrEmpty(_currentDeviceId))
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[Heartbeat] Sending...");
                    var apiService = new ApiService();
                    var result = await apiService.PostAnalyticsAsync("heartbeat", _currentDeviceId);
                    if (result.Success)
                    {
                        System.Diagnostics.Debug.WriteLine("[Heartbeat] Sent successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Heartbeat] Failed: {result.Error}");
                    }
                }
                catch (Exception ex) 
                { 
                    System.Diagnostics.Debug.WriteLine($"[Heartbeat] Exception: {ex.Message}");
                }
            }
        };
        _heartbeatTimer.Start();
        System.Diagnostics.Debug.WriteLine("[App] Heartbeat tracking started (30s interval)");
    }

    /// <summary>
    /// Sync SQLite local data to Web Admin (reviews, check-ins, saved POIs)
    /// </summary>
    private async Task SyncLocalDataToWebAdminAsync()
    {
        if (string.IsNullOrEmpty(_currentDeviceId)) return;
        
        try
        {
            System.Diagnostics.Debug.WriteLine("[Sync] SQLite sync disabled for now - using analytics only");
            // TODO: Add sync code when DatabaseService methods are ready
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sync] Error: {ex.Message}");
        }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        System.Diagnostics.Debug.WriteLine("[App] OnSleep - App going to background");
        
        // Stop heartbeat timer when app goes to background
        if (_heartbeatTimer != null)
        {
            _heartbeatTimer.Stop();
            System.Diagnostics.Debug.WriteLine("[App] Heartbeat timer stopped (app backgrounded)");
        }
        
        // Stop geofence tracking when app is backgrounded or killed
        var geofenceEngine = ServiceProviderHelper.GetService<IGeofenceEngine>();
        if (geofenceEngine?.IsEnabled == true)
        {
            geofenceEngine.Disable();
            System.Diagnostics.Debug.WriteLine("[App] Geofence disabled (app backgrounded)");
        }
        
        // Stop location tracking
        var locationService = ServiceProviderHelper.GetService<ILocationService>();
        if (locationService?.IsTracking == true)
        {
            _ = locationService.StopTrackingAsync();
            System.Diagnostics.Debug.WriteLine("[App] Location tracking stopped (app backgrounded)");
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("[App] OnResume - App returning to foreground");
        
        // Track app open when returning from background
        if (!string.IsNullOrEmpty(_currentDeviceId))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await StartOrContinueSessionAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] Failed to track resume: {ex.Message}");
                }
            });
        }
        
        // Geofence will be re-enabled in MainPage.OnAppearing if needed
    }
}
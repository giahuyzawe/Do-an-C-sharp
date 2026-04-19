using FoodStreetGuide.Services;
using FoodStreetGuide.Models;
using FoodStreetGuide.Database;

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
                
                // Get or create DeviceId
                _currentDeviceId = Preferences.Get("DeviceId", string.Empty);
                if (string.IsNullOrEmpty(_currentDeviceId))
                {
                    _currentDeviceId = Guid.NewGuid().ToString();
                    Preferences.Set("DeviceId", _currentDeviceId);
                }
                
                // Record app visit (local)
                await databaseService.RecordAppVisitAsync(_currentDeviceId, DateTime.Now, AppInfo.Version.ToString(), DeviceInfo.Platform.ToString());
                System.Diagnostics.Debug.WriteLine($"[App] Visit recorded for device: {_currentDeviceId.Substring(0, 8)}...");
                
                // Send to Web Admin API
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var apiService = new ApiService();
                        var result = await apiService.PostAnalyticsAsync("app_visit", _currentDeviceId);
                        if (result.Success)
                        {
                            System.Diagnostics.Debug.WriteLine("[App] Analytics sent to Web Admin");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[App] Failed to send analytics: {result.Error}");
                        }
                    }
                    catch (Exception apiEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App] API error: {apiEx.Message}");
                    }
                });
                
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
                System.Diagnostics.Debug.WriteLine($"[App] App open recorded in existing session: {_currentSessionId.Substring(0, 8)}...");
                return;
            }
        }

        // Start new session
        _currentSessionId = await Database.StartNewSessionAsync(_currentDeviceId);
        Preferences.Set("CurrentSessionId", _currentSessionId);
        System.Diagnostics.Debug.WriteLine($"[App] New session started: {_currentSessionId.Substring(0, 8)}...");
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        System.Diagnostics.Debug.WriteLine("[App] OnSleep - App going to background");
        
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
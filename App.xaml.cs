using FoodStreetGuide.Services;

namespace FoodStreetGuide;

public partial class App : Application
{
    public static DatabaseService Database { get; private set; }

    public App(DatabaseService databaseService)
    {
        InitializeComponent();

        Database = databaseService;

        //Database.Init();

        // Record app visit for analytics (no login required)
        _ = Task.Run(async () =>
        {
            try
            {
                await databaseService.Init();
                var deviceId = Preferences.Get("DeviceId", string.Empty);
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = Guid.NewGuid().ToString();
                    Preferences.Set("DeviceId", deviceId);
                }
                await databaseService.RecordAppVisitAsync(deviceId);
                System.Diagnostics.Debug.WriteLine($"[App] Visit recorded for device: {deviceId.Substring(0, 8)}...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Failed to record visit: {ex.Message}");
            }
        });

        MainPage = new AppShell();
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
        // Geofence will be re-enabled in MainPage.OnAppearing if needed
    }
}
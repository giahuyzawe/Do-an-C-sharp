using Android.Content;
using Android.App;
using Android.Locations;
using Android.OS;
using FoodStreetGuide.Services;

namespace FoodStreetGuide.Platforms.Android;

public class LocationServiceAndroid : ILocationService
{
    private LocationBroadcastReceiver? _receiver;
    private bool _isTracking;

    public event EventHandler<LocationUpdatedEventArgs>? LocationUpdated;

    public bool IsTracking => _isTracking;

    public Task StartTrackingAsync()
    {
        if (IsTracking) return Task.CompletedTask;

        var context = Platform.CurrentActivity;
        if (context == null) return Task.CompletedTask;

        // Register broadcast receiver
        _receiver = new LocationBroadcastReceiver(this);
        var filter = new IntentFilter("LOCATION_UPDATE");
        context.RegisterReceiver(_receiver, filter);

        // Start foreground service
        var intent = new Intent(context, typeof(LocationTrackingService));
        context.StartForegroundService(intent);

        _isTracking = true;
        return Task.CompletedTask;
    }

    public Task StopTrackingAsync()
    {
        var context = Platform.CurrentActivity;
        if (context != null)
        {
            // Always try to stop service (in case state wasn't restored properly)
            var intent = new Intent(context, typeof(LocationTrackingService));
            context.StopService(intent);

            // Unregister receiver
            if (_receiver != null)
            {
                try
                {
                    context.UnregisterReceiver(_receiver);
                }
                catch (Exception)
                {
                    // Receiver might not be registered
                }
                _receiver = null;
            }
        }

        _isTracking = false;
        return Task.CompletedTask;
    }

    // Check if service is running and restore state
    public void CheckAndRestoreState()
    {
        var context = Platform.CurrentActivity;
        if (context == null) return;

        if (IsServiceRunning(context, typeof(LocationTrackingService)))
        {
            // Service is running, restore state
            _isTracking = true;
            
            // Re-register receiver
            if (_receiver == null)
            {
                _receiver = new LocationBroadcastReceiver(this);
                var filter = new IntentFilter("LOCATION_UPDATE");
                context.RegisterReceiver(_receiver, filter);
            }
        }
        else
        {
            // Service is NOT running, ensure _isTracking is false
            _isTracking = false;
            
            // Cleanup receiver if exists
            if (_receiver != null)
            {
                try
                {
                    context.UnregisterReceiver(_receiver);
                }
                catch (Exception)
                {
                    // Receiver might not be registered
                }
                _receiver = null;
            }
        }
    }

    private bool IsServiceRunning(Context context, Type serviceClass)
    {
        var manager = (ActivityManager)context.GetSystemService(Context.ActivityService);
        if (manager == null) return false;

        var services = manager.GetRunningServices(int.MaxValue);
        foreach (var service in services)
        {
            if (service.Service.ClassName == serviceClass.FullName)
            {
                return true;
            }
        }
        return false;
    }

    internal void OnLocationReceived(double latitude, double longitude, float accuracy)
    {
        LocationUpdated?.Invoke(this, new LocationUpdatedEventArgs
        {
            Latitude = latitude,
            Longitude = longitude,
            Accuracy = accuracy,
            Timestamp = DateTime.Now
        });
    }

    public async Task<LocationUpdatedEventArgs?> GetCurrentLocationAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[GetCurrentLocationAsync] Starting...");
            
            var context = Platform.CurrentActivity;
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine("[GetCurrentLocationAsync] Context is null");
                return null;
            }

            // Check permission
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            System.Diagnostics.Debug.WriteLine($"[GetCurrentLocationAsync] Permission status: {status}");
            
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                System.Diagnostics.Debug.WriteLine($"[GetCurrentLocationAsync] Permission after request: {status}");
                
                if (status != PermissionStatus.Granted)
                {
                    System.Diagnostics.Debug.WriteLine("[GetCurrentLocationAsync] Permission denied");
                    return null;
                }
            }

            // Use MAUI Geolocation for better accuracy
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            System.Diagnostics.Debug.WriteLine("[GetCurrentLocationAsync] Requesting location...");
            
            var location = await Microsoft.Maui.Devices.Sensors.Geolocation.GetLocationAsync(request);
            
            if (location != null)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCurrentLocationAsync] Got location: {location.Latitude}, {location.Longitude}");
                return new LocationUpdatedEventArgs
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Accuracy = location.Accuracy ?? 0,
                    Timestamp = DateTime.Now
                };
            }
            
            System.Diagnostics.Debug.WriteLine("[GetCurrentLocationAsync] Location is null");
            
            // Fallback to last known location
            var locationManager = (LocationManager?)context.GetSystemService(Context.LocationService);
            if (locationManager != null)
            {
                var lastKnown = locationManager.GetLastKnownLocation(LocationManager.GpsProvider)
                    ?? locationManager.GetLastKnownLocation(LocationManager.NetworkProvider);
                
                if (lastKnown != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetCurrentLocationAsync] Using last known: {lastKnown.Latitude}, {lastKnown.Longitude}");
                    return new LocationUpdatedEventArgs
                    {
                        Latitude = lastKnown.Latitude,
                        Longitude = lastKnown.Longitude,
                        Accuracy = lastKnown.Accuracy,
                        Timestamp = DateTime.Now
                    };
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetCurrentLocationAsync] Error: {ex.Message}");
            return null;
        }
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public class LocationBroadcastReceiver : BroadcastReceiver
{
    private readonly LocationServiceAndroid? _service;

    public LocationBroadcastReceiver() { }

    public LocationBroadcastReceiver(LocationServiceAndroid service)
    {
        _service = service;
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action == "LOCATION_UPDATE")
        {
            var lat = intent.GetDoubleExtra("latitude", 0);
            var lng = intent.GetDoubleExtra("longitude", 0);
            var accuracy = intent.GetFloatExtra("accuracy", 0);

            _service?.OnLocationReceived(lat, lng, accuracy);
        }
    }
}

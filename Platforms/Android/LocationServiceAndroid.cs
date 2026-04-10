using Android.Content;
using Android.App;
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

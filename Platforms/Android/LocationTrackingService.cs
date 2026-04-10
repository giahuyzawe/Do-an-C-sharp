using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Android.Gms.Location;

namespace FoodStreetGuide.Platforms.Android;

[Service(Exported = true, ForegroundServiceType = (global::Android.Content.PM.ForegroundService)8)]  // LOCATION = 8
public class LocationTrackingService : Service
{
    private LocationCallback? _locationCallback;
    private global::Android.Gms.Location.IFusedLocationProviderClient? _fusedLocationClient;
    private const int NOTIFICATION_ID = 1001;
    private const string CHANNEL_ID = "location_tracking_channel";

    public override void OnCreate()
    {
        base.OnCreate();
        _fusedLocationClient = (global::Android.Gms.Location.IFusedLocationProviderClient)global::Android.Gms.Location.LocationServices.GetFusedLocationProviderClient(this);
    }

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        
        var notification = CreateNotification();
        StartForeground(NOTIFICATION_ID, notification);

        StartLocationUpdates();

        return StartCommandResult.NotSticky;
    }

    private void StartLocationUpdates()
    {
        // Use modern LocationRequest.Builder API
        var locationRequest = new global::Android.Gms.Location.LocationRequest.Builder(
            global::Android.Gms.Location.Priority.PriorityHighAccuracy, 5000)
            .SetMinUpdateIntervalMillis(2000)
            .Build();

        var locationCallback = new LocationCallbackHelper(this);
        _locationCallback = locationCallback;
        
        try
        {
            if (_fusedLocationClient != null)
            {
                _fusedLocationClient.RequestLocationUpdates(locationRequest, locationCallback, Looper.MainLooper!);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Location tracking error: {ex.Message}");
        }
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        base.OnDestroy();
        StopLocationUpdates();
    }

    public override void OnTaskRemoved(Intent? rootIntent)
    {
        // Stop service when app is swiped away from recent apps
        StopSelfResult(1);
        base.OnTaskRemoved(rootIntent);
    }

    private void StopLocationUpdates()
    {
        if (_locationCallback != null)
        {
            _fusedLocationClient?.RemoveLocationUpdates(_locationCallback);
        }
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                CHANNEL_ID,
                "Location Tracking",
                NotificationImportance.Low)
            {
                Description = "Tracking your location for nearby restaurant alerts"
            };

            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    private Notification CreateNotification()
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, CHANNEL_ID)
            .SetContentTitle("FoodStreetGuide")
            .SetContentText("Đang theo dõi vị trí để phát hiện quán ăn gần bạn...")
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuMyLocation)
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .Build();
    }

    private class LocationCallbackHelper : LocationCallback
    {
        private readonly LocationTrackingService _service;

        public LocationCallbackHelper(LocationTrackingService service)
        {
            _service = service;
        }

        public override void OnLocationResult(global::Android.Gms.Location.LocationResult result)
        {
            base.OnLocationResult(result);
            
            if (result.LastLocation != null)
            {
                var location = result.LastLocation;
                
                // Broadcast location update to the app
                var intent = new Intent("LOCATION_UPDATE");
                intent.PutExtra("latitude", location.Latitude);
                intent.PutExtra("longitude", location.Longitude);
                intent.PutExtra("accuracy", location.Accuracy);
                _service.SendBroadcast(intent);
                
                System.Diagnostics.Debug.WriteLine($"Location: {location.Latitude}, {location.Longitude}");
            }
        }
    }
}

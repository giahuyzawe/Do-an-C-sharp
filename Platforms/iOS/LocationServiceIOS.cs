using CoreLocation;
using FoodStreetGuide.Services;

namespace FoodStreetGuide.Platforms.iOS;

public class LocationServiceIOS : ILocationService
{
    private CLLocationManager? _locationManager;
    private bool _isTracking;
    
    public event EventHandler<LocationUpdatedEventArgs>? LocationUpdated;
    public bool IsTracking => _isTracking;

    public Task StartTrackingAsync()
    {
        if (_isTracking) return Task.CompletedTask;

        _locationManager = new CLLocationManager
        {
            DesiredAccuracy = CLLocation.AccuracyBest,
            DistanceFilter = 10, // meters
            AllowsBackgroundLocationUpdates = true,
            PausesLocationUpdatesAutomatically = false,
            ActivityType = CLActivityType.Fitness // Walking/touring
        };

        _locationManager.AuthorizationChanged += OnAuthorizationChanged;
        _locationManager.LocationsUpdated += OnLocationsUpdated;
        _locationManager.Failed += OnLocationFailed;

        // Request permission
        _locationManager.RequestAlwaysAuthorization();

        _locationManager.StartUpdatingLocation();
        _isTracking = true;

        System.Diagnostics.Debug.WriteLine("[LocationServiceIOS] Started tracking");
        return Task.CompletedTask;
    }

    public Task StopTrackingAsync()
    {
        if (_locationManager == null) return Task.CompletedTask;

        _locationManager.StopUpdatingLocation();
        _locationManager.AuthorizationChanged -= OnAuthorizationChanged;
        _locationManager.LocationsUpdated -= OnLocationsUpdated;
        _locationManager.Failed -= OnLocationFailed;
        
        _locationManager.Dispose();
        _locationManager = null;
        _isTracking = false;

        System.Diagnostics.Debug.WriteLine("[LocationServiceIOS] Stopped tracking");
        return Task.CompletedTask;
    }

    public async Task<LocationUpdatedEventArgs?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted) return null;
            }

            // Use MAUI Geolocation
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            var location = await Microsoft.Maui.Devices.Sensors.Geolocation.GetLocationAsync(request);

            if (location != null)
            {
                return new LocationUpdatedEventArgs
                {
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Accuracy = location.Accuracy ?? 0,
                    Timestamp = DateTime.Now
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocationServiceIOS] Error: {ex.Message}");
        }
        return null;
    }

    private void OnAuthorizationChanged(object? sender, CLAuthorizationChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[LocationServiceIOS] Auth changed: {e.Status}");
    }

    private void OnLocationsUpdated(object? sender, CLLocationsUpdatedEventArgs e)
    {
        var location = e.Locations.LastOrDefault();
        if (location == null) return;

        LocationUpdated?.Invoke(this, new LocationUpdatedEventArgs
        {
            Latitude = location.Coordinate.Latitude,
            Longitude = location.Coordinate.Longitude,
            Accuracy = location.HorizontalAccuracy,
            Timestamp = DateTime.Now
        });

        System.Diagnostics.Debug.WriteLine($"[LocationServiceIOS] Location: {location.Coordinate.Latitude}, {location.Coordinate.Longitude}");
    }

    private void OnLocationFailed(object? sender, NSErrorEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[LocationServiceIOS] Failed: {e.Error}");
    }
}

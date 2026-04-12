namespace FoodStreetGuide.Services;

public interface ILocationService
{
    event EventHandler<LocationUpdatedEventArgs>? LocationUpdated;
    Task StartTrackingAsync();
    Task StopTrackingAsync();
    bool IsTracking { get; }
    Task<LocationUpdatedEventArgs?> GetCurrentLocationAsync();
}

public class LocationUpdatedEventArgs : EventArgs
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public DateTime Timestamp { get; set; }
}

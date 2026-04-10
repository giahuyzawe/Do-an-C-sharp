using Microsoft.Maui.Maps;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public interface IGeofenceEngine
{
    event EventHandler<GeofenceEventArgs>? POIEntered;
    event EventHandler<GeofenceEventArgs>? POIExited;
    event EventHandler<NearestPOIChangedEventArgs>? NearestPOIChanged;

    bool IsEnabled { get; }
    double DebounceMeters { get; set; }
    TimeSpan CooldownDuration { get; set; }
    
    void Enable();
    void Disable();
    void UpdateLocation(double latitude, double longitude);
    void SetPOIs(List<POI> pois);
}

public class GeofenceEventArgs : EventArgs
{
    public required POI POI { get; set; }
    public required double DistanceMeters { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class NearestPOIChangedEventArgs : EventArgs
{
    public POI? PreviousPOI { get; set; }
    public POI? NewPOI { get; set; }
    public double DistanceMeters { get; set; }
}

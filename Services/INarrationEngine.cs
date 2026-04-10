using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public interface INarrationEngine
{
    void Enable();
    void Disable();
    bool IsEnabled { get; }
    
    // Queue a narration for a POI
    void QueueNarration(POI poi, double distanceMeters, double speedMs = 0);
    
    // Cancel all pending narrations
    void CancelAll();
    
    // Cancel narrations for a specific POI
    void CancelForPOI(int poiId);
    
    // Settings
    double MaxNarrationDistance { get; set; }  // Max distance to narrate (default 200m)
    double MinSpeedForNarration { get; set; }  // Min speed to narrate (default 0.5 m/s = 1.8 km/h)
    double MaxSpeedForNarration { get; set; }  // Max speed to narrate (default 20 m/s = 72 km/h)
    TimeSpan NarrationCooldown { get; set; }   // Cooldown between narrations (default 30s)
}

public class NarrationItem
{
    public POI POI { get; set; } = null!;
    public double DistanceMeters { get; set; }
    public double SpeedMs { get; set; }
    public DateTime Timestamp { get; set; }
    public int Priority { get; set; }  // Lower = higher priority
}

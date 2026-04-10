using Microsoft.Maui.Maps;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public class GeofenceEngine : IGeofenceEngine
{
    private List<POI> _pois = new();
    private readonly Dictionary<int, DateTime> _lastTriggered = new(); // POI ID -> last trigger time
    private readonly Dictionary<int, bool> _insideGeofence = new(); // POI ID -> is inside
    
    public event EventHandler<GeofenceEventArgs>? POIEntered;
    public event EventHandler<GeofenceEventArgs>? POIExited;
    public event EventHandler<NearestPOIChangedEventArgs>? NearestPOIChanged;

    public bool IsEnabled { get; private set; }
    
    // Configurable cooldown (default 5 minutes)
    public TimeSpan CooldownDuration { get; set; } = TimeSpan.FromSeconds(10);
    // Debounce threshold (prevent flickering at boundary)
    public double DebounceMeters { get; set; } = 10;

    private POI? _currentNearestPOI;

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
        _currentNearestPOI = null;
    }

    public void SetPOIs(List<POI> pois)
    {
        // Preserve _insideGeofence state for existing POIs to prevent re-trigger
        // Only clear _lastTriggered for new cooldown cycle if POI list actually changed
        var currentIds = _pois.Select(p => p.Id).ToHashSet();
        var newIds = pois.Select(p => p.Id).ToHashSet();
        
        // Only clear if the list actually changed
        if (!currentIds.SetEquals(newIds))
        {
            _lastTriggered.Clear();
            _insideGeofence.Clear();
        }
        
        _pois = pois;
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        if (!IsEnabled || _pois.Count == 0) return;

        var userLocation = new Location(latitude, longitude);
        var now = DateTime.Now;

        POI? nearestPOI = null;
        double minDistance = double.MaxValue;
        
        // Find nearest POI and all POIs currently inside
        var insidePOIs = new List<(POI poi, double distance)>();

        foreach (var poi in _pois)
        {
            var poiLocation = new Location(poi.Latitude, poi.Longitude);
            var distance = userLocation.CalculateDistance(poiLocation, DistanceUnits.Kilometers) * 1000;

            System.Diagnostics.Debug.WriteLine($"[Geofence] POI {poi.NameVi}: {distance:F1}m / {poi.Radius}m radius");

            // Track nearest
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPOI = poi;
            }

            // Track all POIs inside radius
            if (distance <= poi.Radius)
            {
                insidePOIs.Add((poi, distance));
            }
            else
            {
                // Check exit for POIs outside
                CheckGeofence(poi, distance, now, isNearest: false);
            }
        }

        // Only trigger the nearest POI among those inside
        if (insidePOIs.Count > 0)
        {
            // Find the closest one among all inside POIs
            var closestInside = insidePOIs.OrderBy(x => x.distance).First();
            
            // Only trigger the nearest, mark others as "not entered" to prevent multiple triggers
            foreach (var (poi, distance) in insidePOIs)
            {
                bool isNearest = poi.Id == closestInside.poi.Id;
                CheckGeofence(poi, distance, now, isNearest);
            }
        }

        // Notify nearest POI changed - per-POI cooldown
        if (nearestPOI != null && minDistance <= nearestPOI.Radius)
        {
            var lastTriggered = _lastTriggered.TryGetValue(nearestPOI.Id, out var last) ? last : DateTime.MinValue;
            var cooldownElapsed = now - lastTriggered >= CooldownDuration;
            
            // Skip only if within cooldown for THIS specific POI
            if (!cooldownElapsed && _lastTriggered.ContainsKey(nearestPOI.Id))
            {
                // This POI was triggered recently - skip
                System.Diagnostics.Debug.WriteLine($"[Geofence] {nearestPOI.NameVi} in cooldown, skipped");
            }
            else
            {
                var args = new NearestPOIChangedEventArgs
                {
                    PreviousPOI = _currentNearestPOI,
                    NewPOI = nearestPOI,
                    DistanceMeters = minDistance
                };
                _currentNearestPOI = nearestPOI;
                _lastTriggered[nearestPOI.Id] = now;
                NearestPOIChanged?.Invoke(this, args);
                System.Diagnostics.Debug.WriteLine($"[Geofence] Nearest POI changed to: {nearestPOI.NameVi} ({minDistance:F0}m)");
            }
        }
        // Clear current nearest when outside all geofences
        else if (_currentNearestPOI != null && insidePOIs.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Geofence] Left all geofences, clearing nearest POI");
            _currentNearestPOI = null;
        }
    }

    private void CheckGeofence(POI poi, double distanceMeters, DateTime now, bool isNearest)
    {
        var poiId = poi.Id;
        var isInside = distanceMeters <= poi.Radius;
        
        if (!_insideGeofence.ContainsKey(poiId))
            _insideGeofence[poiId] = false;
            
        var wasInside = _insideGeofence[poiId];

        var lastTriggered = _lastTriggered.TryGetValue(poiId, out var last) ? last : DateTime.MinValue;
        var cooldownElapsed = now - lastTriggered >= CooldownDuration;

        System.Diagnostics.Debug.WriteLine($"[Geofence] Check {poi.NameVi}: wasInside={wasInside}, isInside={isInside}, isNearest={isNearest}, cooldown={cooldownElapsed}");

        // ENTER event - ONLY trigger if this is the nearest POI among those inside
        if (isInside && !wasInside && cooldownElapsed && isNearest)
        {
            _insideGeofence[poiId] = true;
            _lastTriggered[poiId] = now;
            System.Diagnostics.Debug.WriteLine($"[Geofence] *** ENTERED {poi.NameVi} (nearest)!");
            POIEntered?.Invoke(this, new GeofenceEventArgs
            {
                POI = poi,
                DistanceMeters = distanceMeters
            });
        }
        // Mark as inside without triggering if not nearest
        else if (isInside && !wasInside && !isNearest)
        {
            _insideGeofence[poiId] = true;
            System.Diagnostics.Debug.WriteLine($"[Geofence] Inside {poi.NameVi} but not nearest - no trigger");
        }
        // EXIT event
        else if (!isInside && wasInside && distanceMeters > poi.Radius + DebounceMeters)
        {
            _insideGeofence[poiId] = false;
            System.Diagnostics.Debug.WriteLine($"[Geofence] *** EXITED {poi.NameVi}");
            POIExited?.Invoke(this, new GeofenceEventArgs
            {
                POI = poi,
                DistanceMeters = distanceMeters
            });
        }
    }

    // Check if can narrate (cooldown expired)
    public bool CanNarrate(int poiId)
    {
        if (!_lastTriggered.TryGetValue(poiId, out var last))
            return true;
        return DateTime.Now - last >= CooldownDuration;
    }
}

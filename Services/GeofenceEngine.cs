using Microsoft.Maui.Maps;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public class GeofenceEngine : IGeofenceEngine
{
    private List<POI> _pois = new();
    private readonly ISettingsService? _settingsService;
    
    // Track when each POI was last triggered (for cooldown)
    private readonly Dictionary<int, DateTime> _lastTriggered = new();
    
    // Track which POIs user is currently inside
    private readonly Dictionary<int, bool> _insideGeofence = new();
    
    // Track which POI was last triggered to avoid same POI
    private int _lastTriggeredPOIId = 0;

    public GeofenceEngine(ISettingsService? settingsService = null)
    {
        _settingsService = settingsService;
    }
    
    public event EventHandler<GeofenceEventArgs>? POIEntered;
    public event EventHandler<GeofenceEventArgs>? POIExited;
    public event EventHandler<NearestPOIChangedEventArgs>? NearestPOIChanged;

    public bool IsEnabled { get; private set; }
    public int POICount => _pois.Count;
    
    // Configurable cooldown (default 5 minutes)
    public TimeSpan CooldownDuration { get; set; } = TimeSpan.FromMinutes(5);
    
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
        var currentIds = _pois.Select(p => p.Id).ToHashSet();
        var newIds = pois.Select(p => p.Id).ToHashSet();
        
        // Only clear if the list actually changed
        if (!currentIds.SetEquals(newIds))
        {
            _lastTriggered.Clear();
            _insideGeofence.Clear();
            _lastTriggeredPOIId = 0;
        }
        
        _pois = pois;
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        System.Diagnostics.Debug.WriteLine($"[Geofence] UpdateLocation called: {latitude:F6}, {longitude:F6}");
        
        if (!IsEnabled)
        {
            System.Diagnostics.Debug.WriteLine($"[Geofence] ❌ SKIPPED - Geofence not enabled");
            return;
        }
        
        if (_pois.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Geofence] ❌ SKIPPED - No POIs loaded");
            return;
        }

        var userLocation = new Location(latitude, longitude);
        var now = DateTime.Now;
        
        System.Diagnostics.Debug.WriteLine($"[Geofence] Processing {_pois.Count} POIs...");

        // Calculate distances for ALL POIs
        var poiDistances = new List<(POI poi, double distance)>();
        
        foreach (var poi in _pois)
        {
            var poiLocation = new Location(poi.Latitude, poi.Longitude);
            var distance = userLocation.CalculateDistance(poiLocation, DistanceUnits.Kilometers) * 1000;
            poiDistances.Add((poi, distance));
            
            // Log each POI distance and priority for debugging
            var inside = distance <= poi.Radius ? "✓ INSIDE" : "✗ outside";
            System.Diagnostics.Debug.WriteLine($"[Geofence]   {poi.NameVi}: {distance:F0}m / {poi.Radius}m radius [P{poi.Priority}] {inside}");
        }

        // Find nearest POI (regardless of radius)
        var nearest = poiDistances.OrderBy(x => x.distance).First();
        var nearestPOI = nearest.poi;
        var minDistance = nearest.distance;

        // Handle exit events for POIs that user left
        foreach (var (poi, distance) in poiDistances)
        {
            var poiId = poi.Id;
            var isInside = distance <= poi.Radius;
            
            if (!_insideGeofence.ContainsKey(poiId))
                _insideGeofence[poiId] = false;
                
            var wasInside = _insideGeofence[poiId];

            // EXIT event - user left this POI's geofence
            if (!isInside && wasInside && distance > poi.Radius + DebounceMeters)
            {
                _insideGeofence[poiId] = false;
                System.Diagnostics.Debug.WriteLine($"[Geofence] *** EXITED {poi.NameVi}");
                POIExited?.Invoke(this, new GeofenceEventArgs
                {
                    POI = poi,
                    DistanceMeters = distance
                });
                
                // Clear last triggered when user exits, so they can re-enter later
                if (_lastTriggeredPOIId == poiId)
                {
                    System.Diagnostics.Debug.WriteLine($"[Geofence] Cleared last triggered POI ({poi.NameVi})");
                    _lastTriggeredPOIId = 0;
                }
            }
            // Mark as inside (for tracking purposes)
            else if (isInside && !wasInside)
            {
                _insideGeofence[poiId] = true;
            }
        }

        // Find all POIs inside radius, sorted by priority (high to low) then by distance
        var insidePOIs = poiDistances
            .Where(x => x.distance <= x.poi.Radius)
            .OrderByDescending(x => x.poi.Priority)  // Higher priority first (3 > 2 > 1)
            .ThenBy(x => x.distance)  // Then by nearest distance
            .ToList();

        var highestPriorityPOI = insidePOIs.FirstOrDefault();
        System.Diagnostics.Debug.WriteLine($"[Geofence] Inside {insidePOIs.Count} POIs, trigger order: Priority[3→1] + Distance");
        if (highestPriorityPOI.poi != null)
        {
            System.Diagnostics.Debug.WriteLine($"[Geofence] Next trigger candidate: {highestPriorityPOI.poi.NameVi} (P{highestPriorityPOI.poi.Priority}, {highestPriorityPOI.distance:F0}m)");
        }

        // Notify nearest POI changed (for UI highlighting)
        if (insidePOIs.Count > 0)
        {
            var closestInside = insidePOIs.First();
            
            if (_currentNearestPOI?.Id != closestInside.poi.Id)
            {
                var args = new NearestPOIChangedEventArgs
                {
                    PreviousPOI = _currentNearestPOI,
                    NewPOI = closestInside.poi,
                    DistanceMeters = closestInside.distance
                };
                _currentNearestPOI = closestInside.poi;
                NearestPOIChanged?.Invoke(this, args);
                System.Diagnostics.Debug.WriteLine($"[Geofence] UI: Nearest POI changed to: {closestInside.poi.NameVi}");
            }
        }
        else if (_currentNearestPOI != null)
        {
            System.Diagnostics.Debug.WriteLine($"[Geofence] Left all geofences");
            _currentNearestPOI = null;
        }

        // TRIGGER LOGIC: Find highest priority available POI (Priority 3 > 2 > 1, then nearest distance)
        // Example: POI A (P3, 80m) will trigger before POI B (P1, 50m) if both in radius
        TriggerNearestAvailablePOI(insidePOIs, now);
    }

    private void TriggerNearestAvailablePOI(List<(POI poi, double distance)> insidePOIs, DateTime now)
    {
        // SMART MODE: Trigger by PRIORITY first (P3 > P2 > P1), then by DISTANCE
        // Priority 3 POI at 80m will trigger before Priority 1 POI at 50m
        // If highest priority is in cooldown, try next POI in priority+distance order
        System.Diagnostics.Debug.WriteLine($"[Geofence] Finding highest priority available (sorted by P[3→1] + distance)...");

        foreach (var (poi, distance) in insidePOIs)
        {
            var poiId = poi.Id;
            
            // Check cooldown for this specific POI
            var lastTriggered = _lastTriggered.TryGetValue(poiId, out var last) ? last : DateTime.MinValue;
            var cooldownElapsed = now - lastTriggered >= CooldownDuration;
            
            // Skip if this POI is in cooldown
            if (!cooldownElapsed)
            {
                System.Diagnostics.Debug.WriteLine($"[Geofence] SKIP {poi.NameVi} [P{poi.Priority}] (cooldown: {(now - lastTriggered).TotalSeconds:F0}s ago)");
                continue;
            }
            
            // This POI is the nearest one not in cooldown - TRIGGER IT!
            _lastTriggered[poiId] = now;
            _lastTriggeredPOIId = poiId;
            
            System.Diagnostics.Debug.WriteLine($"[Geofence] *** TRIGGERED: {poi.NameVi} (P{poi.Priority}, {distance:F0}m)");
            POIEntered?.Invoke(this, new GeofenceEventArgs
            {
                POI = poi,
                DistanceMeters = distance
            });
            
            // Only trigger ONE POI at a time - the nearest available
            return;
        }
        
        if (insidePOIs.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Geofence] No POI available to trigger (all {insidePOIs.Count} POIs in cooldown)");
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

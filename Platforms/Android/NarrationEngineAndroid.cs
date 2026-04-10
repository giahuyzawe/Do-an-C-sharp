using FoodStreetGuide.Models;
using FoodStreetGuide.Services;
using System.Collections.Concurrent;

namespace FoodStreetGuide.Platforms.Android;

public class NarrationEngineAndroid : INarrationEngine
{
    private readonly ITTSService _ttsService;
    private readonly ISettingsService _settingsService;
    
    private ConcurrentQueue<NarrationItem> _narrationQueue = new();
    private Dictionary<int, DateTime> _lastNarrationTimes = new();
    private bool _isProcessing = false;
    private bool _isEnabled = false;
    private CancellationTokenSource? _processingCts;
    
    // Default settings
    public double MaxNarrationDistance { get; set; } = 200.0;  // meters
    public double MinSpeedForNarration { get; set; } = 0.5;   // m/s (walking speed)
    public double MaxSpeedForNarration { get; set; } = 20.0;  // m/s (72 km/h)
    public TimeSpan NarrationCooldown { get; set; } = TimeSpan.FromSeconds(30);
    
    public bool IsEnabled => _isEnabled;

    public NarrationEngineAndroid(ITTSService ttsService, ISettingsService settingsService)
    {
        _ttsService = ttsService;
        _settingsService = settingsService;
    }

    public void Enable()
    {
        if (_isEnabled) return;
        
        _isEnabled = true;
        _processingCts = new CancellationTokenSource();
        _ = ProcessQueueAsync(_processingCts.Token);
        
        System.Diagnostics.Debug.WriteLine("[NarrationEngine] Enabled");
    }

    public void Disable()
    {
        if (!_isEnabled) return;
        
        _isEnabled = false;
        _processingCts?.Cancel();
        _processingCts?.Dispose();
        _processingCts = null;
        
        CancelAll();
        
        System.Diagnostics.Debug.WriteLine("[NarrationEngine] Disabled");
    }

    public void QueueNarration(POI poi, double distanceMeters, double speedMs = 0)
    {
        if (!_isEnabled) return;
        if (!_settingsService.GetNarrationEnabled()) return;
        if (distanceMeters > MaxNarrationDistance) return;
        if (speedMs < MinSpeedForNarration || speedMs > MaxSpeedForNarration) return;

        // Check cooldown for this POI
        if (_lastNarrationTimes.TryGetValue(poi.Id, out var lastTime))
        {
            var geofenceCooldown = TimeSpan.FromMinutes(_settingsService.GetCooldownMinutes());
            if (DateTime.Now - lastTime < geofenceCooldown)
            {
                System.Diagnostics.Debug.WriteLine($"[NarrationEngine] POI {poi.NameVi} on cooldown, skipping");
                return;
            }
        }

        // Calculate priority (lower = higher priority)
        // Priority = distance (closer = more important) + speed bonus
        int priority = (int)distanceMeters;
        if (speedMs > 5) priority -= 50; // Bonus for faster movement

        var item = new NarrationItem
        {
            POI = poi,
            DistanceMeters = distanceMeters,
            SpeedMs = speedMs,
            Timestamp = DateTime.Now,
            Priority = priority
        };

        // Check if already in queue
        if (_narrationQueue.Any(q => q.POI.Id == poi.Id))
        {
            System.Diagnostics.Debug.WriteLine($"[NarrationEngine] POI {poi.NameVi} already in queue, skipping");
            return;
        }

        _narrationQueue.Enqueue(item);
        System.Diagnostics.Debug.WriteLine($"[NarrationEngine] Queued {poi.NameVi} (dist: {distanceMeters:F0}m, speed: {speedMs:F1}m/s, priority: {priority})");
    }

    public void CancelAll()
    {
        while (_narrationQueue.TryDequeue(out _)) { }
        _ttsService?.StopAsync();
        System.Diagnostics.Debug.WriteLine("[NarrationEngine] Cancelled all narrations");
    }

    public void CancelForPOI(int poiId)
    {
        // Remove from queue (need to rebuild queue without this POI)
        var items = _narrationQueue.ToList().Where(i => i.POI.Id != poiId).ToList();
        _narrationQueue = new ConcurrentQueue<NarrationItem>(items);
        System.Diagnostics.Debug.WriteLine($"[NarrationEngine] Cancelled narrations for POI {poiId}");
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (_isEnabled && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait a bit between narrations
                await Task.Delay(NarrationCooldown, cancellationToken);
                
                if (_narrationQueue.IsEmpty) continue;
                if (_isProcessing) continue;
                
                // Get highest priority item (sort by priority)
                var items = _narrationQueue.ToList().OrderBy(i => i.Priority).ToList();
                if (items.Count == 0) continue;
                
                var item = items.First();
                
                // Remove it from queue
                _narrationQueue = new ConcurrentQueue<NarrationItem>(items.Skip(1));
                
                await ProcessNarrationAsync(item, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NarrationEngine] Error: {ex.Message}");
            }
        }
    }

    private async Task ProcessNarrationAsync(NarrationItem item, CancellationToken cancellationToken)
    {
        _isProcessing = true;
        
        try
        {
            var poi = item.POI;
            var voice = _settingsService.GetTTSVoice();
            var rate = _settingsService.GetTTSRate();
            
            string message;
            if (voice.StartsWith("en"))
            {
                message = $"You are approaching {poi.NameEn}. {poi.DescriptionEn}";
            }
            else
            {
                message = $"Bạn đang đến gần {poi.NameVi}. {poi.DescriptionVi}";
            }
            
            System.Diagnostics.Debug.WriteLine($"[NarrationEngine] Speaking: {poi.NameVi} (dist: {item.DistanceMeters:F0}m)");
            
            await _ttsService.SpeakAsync(message, voice);
            
            // Record narration time
            _lastNarrationTimes[poi.Id] = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine($"[NarrationEngine] Completed: {poi.NameVi}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NarrationEngine] Error speaking: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
}

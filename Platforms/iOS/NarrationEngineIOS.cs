using AVFoundation;
using FoodStreetGuide.Models;
using FoodStreetGuide.Services;
using System.Collections.Concurrent;

namespace FoodStreetGuide.Platforms.iOS;

public class NarrationEngineIOS : INarrationEngine
{
    private readonly ITTSService _ttsService;
    private readonly ISettingsService _settingsService;
    
    private ConcurrentQueue<NarrationItem> _narrationQueue = new();
    private Dictionary<int, DateTime> _lastNarrationTimes = new();
    private bool _isProcessing = false;
    private bool _isEnabled = false;
    private CancellationTokenSource? _processingCts;
    
    // Default settings
    public double MaxNarrationDistance { get; set; } = 200.0;
    public double MinSpeedForNarration { get; set; } = 0.5;
    public double MaxSpeedForNarration { get; set; } = 20.0;
    public TimeSpan NarrationCooldown { get; set; } = TimeSpan.FromSeconds(30);
    
    public bool IsEnabled => _isEnabled;

    public NarrationEngineIOS(ITTSService ttsService, ISettingsService settingsService)
    {
        _ttsService = ttsService;
        _settingsService = settingsService;
        
        // Configure audio session for background playback
        ConfigureAudioSession();
    }

    private void ConfigureAudioSession()
    {
        var audioSession = AVAudioSession.SharedInstance();
        audioSession.SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.DuckOthers);
        audioSession.SetActive(true);
    }

    public void Enable()
    {
        if (_isEnabled) return;
        
        _isEnabled = true;
        _processingCts = new CancellationTokenSource();
        _ = ProcessQueueAsync(_processingCts.Token);
        
        System.Diagnostics.Debug.WriteLine("[NarrationEngineIOS] Enabled");
    }

    public void Disable()
    {
        if (!_isEnabled) return;
        
        _isEnabled = false;
        _processingCts?.Cancel();
        _processingCts?.Dispose();
        _processingCts = null;
        
        CancelAll();
        
        System.Diagnostics.Debug.WriteLine("[NarrationEngineIOS] Disabled");
    }

    public void QueueNarration(POI poi, double distanceMeters, double speedMs = 0)
    {
        if (!_isEnabled) return;
        if (!_settingsService.GetNarrationEnabled()) return;
        if (distanceMeters > MaxNarrationDistance) return;
        if (speedMs < MinSpeedForNarration || speedMs > MaxSpeedForNarration) return;

        // Check cooldown
        if (_lastNarrationTimes.TryGetValue(poi.Id, out var lastTime))
        {
            var geofenceCooldown = TimeSpan.FromMinutes(_settingsService.GetCooldownMinutes());
            if (DateTime.Now - lastTime < geofenceCooldown)
            {
                System.Diagnostics.Debug.WriteLine($"[NarrationEngineIOS] POI {poi.NameVi} on cooldown");
                return;
            }
        }

        // Calculate priority
        int priority = (int)distanceMeters;
        if (speedMs > 5) priority -= 50;

        var item = new NarrationItem
        {
            POI = poi,
            DistanceMeters = distanceMeters,
            SpeedMs = speedMs,
            Timestamp = DateTime.Now,
            Priority = priority
        };

        if (_narrationQueue.Any(q => q.POI.Id == poi.Id))
        {
            System.Diagnostics.Debug.WriteLine($"[NarrationEngineIOS] POI {poi.NameVi} already in queue");
            return;
        }

        _narrationQueue.Enqueue(item);
        System.Diagnostics.Debug.WriteLine($"[NarrationEngineIOS] Queued {poi.NameVi}");
    }

    public void CancelAll()
    {
        while (_narrationQueue.TryDequeue(out _)) { }
        _ttsService?.StopAsync();
        System.Diagnostics.Debug.WriteLine("[NarrationEngineIOS] Cancelled all");
    }

    public void CancelForPOI(int poiId)
    {
        var items = _narrationQueue.ToList().Where(i => i.POI.Id != poiId).ToList();
        _narrationQueue = new ConcurrentQueue<NarrationItem>(items);
        System.Diagnostics.Debug.WriteLine($"[NarrationEngineIOS] Cancelled for POI {poiId}");
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (_isEnabled && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(NarrationCooldown, cancellationToken);
                
                if (_narrationQueue.IsEmpty) continue;
                if (_isProcessing) continue;
                
                var items = _narrationQueue.ToList().OrderBy(i => i.Priority).ToList();
                if (items.Count == 0) continue;
                
                var item = items.First();
                _narrationQueue = new ConcurrentQueue<NarrationItem>(items.Skip(1));
                
                await ProcessNarrationAsync(item, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NarrationEngineIOS] Error: {ex.Message}");
            }
        }
    }

    private async Task ProcessNarrationAsync(NarrationItem item, CancellationToken cancellationToken)
    {
        _isProcessing = true;
        
        try
        {
            var poi = item.POI;
            var language = _settingsService.GetLanguage();
            var rate = _settingsService.GetTTSRate();
            
            string message;
            string ttsVoice;
            if (language == "en")
            {
                message = $"You are approaching {poi.NameEn}. {poi.DescriptionEn}";
                ttsVoice = "en-US";
            }
            else
            {
                message = $"Bạn đang đến gần {poi.NameVi}. {poi.DescriptionVi}";
                ttsVoice = "vi-VN";
            }
            
            System.Diagnostics.Debug.WriteLine($"[NarrationEngineIOS] Speaking: {poi.NameVi}");
            
            await _ttsService.SpeakAsync(message, ttsVoice);
            
            _lastNarrationTimes[poi.Id] = DateTime.Now;
            
            System.Diagnostics.Debug.WriteLine($"[NarrationEngineIOS] Completed: {poi.NameVi}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NarrationEngineIOS] Error: {ex.Message}");
        }
        finally
        {
            _isProcessing = false;
        }
    }
}

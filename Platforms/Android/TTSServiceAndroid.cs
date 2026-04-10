using FoodStreetGuide.Services;
using Java.Util;
using AndroidTTS = Android.Speech.Tts.TextToSpeech;
using OperationResult = Android.Speech.Tts.OperationResult;

namespace FoodStreetGuide.Platforms.Android;

public class TTSServiceAndroid : Java.Lang.Object, ITTSService, AndroidTTS.IOnInitListener
{
    private AndroidTTS? _tts;
    private bool _isInitialized;
    private string _pendingText = "";
    private string _pendingLanguage = "vi-VN";

    public TTSServiceAndroid()
    {
        var context = Platform.CurrentActivity;
        _tts = new AndroidTTS(context, this);
    }

    public void OnInit(OperationResult status)
    {
        if (status == OperationResult.Success)
        {
            _isInitialized = true;
            if (!string.IsNullOrEmpty(_pendingText))
            {
                SpeakAsync(_pendingText, _pendingLanguage);
            }
        }
    }

    public async Task SpeakAsync(string text, string language = "vi-VN")
    {
        System.Diagnostics.Debug.WriteLine($"[TTSService] SpeakAsync START - Text: {text}, Language: {language}");
        
        if (!_isInitialized)
        {
            System.Diagnostics.Debug.WriteLine($"[TTSService] Not initialized, queuing text: {text}");
            _pendingText = text;
            _pendingLanguage = language;
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[TTSService] Stopping any current speech");
            await StopAsync();
            
            var locale = new Java.Util.Locale(language.Split('-')[0]);
            _tts?.SetLanguage(locale);
            
            // Get rate from settings and apply
            var settingsService = ServiceProviderHelper.GetService<ISettingsService>();
            float rate = settingsService?.GetTTSRate() ?? 1.0f;
            _tts?.SetSpeechRate(rate);
            System.Diagnostics.Debug.WriteLine($"[TTSService] Speech rate set to: {rate}");
            
            // Clear any pending text since we're speaking now
            _pendingText = "";
            
            System.Diagnostics.Debug.WriteLine($"[TTSService] Speaking: {text}");
            _tts?.Speak(text, global::Android.Speech.Tts.QueueMode.Flush, null, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TTSService] Exception: {ex.Message}");
        }
        
        System.Diagnostics.Debug.WriteLine($"[TTSService] SpeakAsync END - Text: {text}");
    }

    public Task StopAsync()
    {
        _tts?.Stop();
        return Task.CompletedTask;
    }

    public bool IsSpeaking => _tts?.IsSpeaking ?? false;
}

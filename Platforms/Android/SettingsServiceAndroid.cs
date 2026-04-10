using FoodStreetGuide.Services;

namespace FoodStreetGuide.Platforms.Android;

public class SettingsServiceAndroid : ISettingsService
{
    // Default values
    private const double DefaultRadius = 100.0;
    private const int DefaultCooldown = 5;
    private const string DefaultVoice = "vi-VN";
    private const float DefaultRate = 1.0f;
    private const bool DefaultNarrationEnabled = true;
    private const int DefaultGPSInterval = 5000; // 5 seconds
    private const int DefaultGPSAccuracy = 100; // 100 meters

    // Keys
    private const string RadiusKey = "GeofenceRadius";
    private const string CooldownKey = "CooldownMinutes";
    private const string VoiceKey = "TTSVoice";
    private const string RateKey = "TTSRate";
    private const string NarrationEnabledKey = "NarrationEnabled";
    private const string GPSIntervalKey = "GPSIntervalMs";
    private const string GPSAccuracyKey = "GPSAccuracy";

    public double GetGeofenceRadius()
    {
        return Preferences.Get(RadiusKey, DefaultRadius);
    }

    public void SetGeofenceRadius(double radius)
    {
        Preferences.Set(RadiusKey, radius);
    }

    public int GetCooldownMinutes()
    {
        return Preferences.Get(CooldownKey, DefaultCooldown);
    }

    public void SetCooldownMinutes(int minutes)
    {
        Preferences.Set(CooldownKey, minutes);
    }

    public bool GetNarrationEnabled()
    {
        return Preferences.Get(NarrationEnabledKey, DefaultNarrationEnabled);
    }

    public void SetNarrationEnabled(bool enabled)
    {
        Preferences.Set(NarrationEnabledKey, enabled);
    }

    public int GetGPSIntervalMs()
    {
        return Preferences.Get(GPSIntervalKey, DefaultGPSInterval);
    }

    public void SetGPSIntervalMs(int intervalMs)
    {
        Preferences.Set(GPSIntervalKey, intervalMs);
    }

    public int GetGPSAccuracy()
    {
        return Preferences.Get(GPSAccuracyKey, DefaultGPSAccuracy);
    }

    public void SetGPSAccuracy(int accuracy)
    {
        Preferences.Set(GPSAccuracyKey, accuracy);
    }

    public string GetTTSVoice()
    {
        return Preferences.Get(VoiceKey, DefaultVoice);
    }

    public void SetTTSVoice(string voice)
    {
        Preferences.Set(VoiceKey, voice);
    }

    public float GetTTSRate()
    {
        return Preferences.Get(RateKey, DefaultRate);
    }

    public void SetTTSRate(float rate)
    {
        Preferences.Set(RateKey, rate);
    }

    public void ResetToDefaults()
    {
        SetGeofenceRadius(DefaultRadius);
        SetCooldownMinutes(DefaultCooldown);
        SetNarrationEnabled(DefaultNarrationEnabled);
        SetGPSIntervalMs(DefaultGPSInterval);
        SetGPSAccuracy(DefaultGPSAccuracy);
        SetTTSVoice(DefaultVoice);
        SetTTSRate(DefaultRate);
    }
}

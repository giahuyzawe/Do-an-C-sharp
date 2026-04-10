namespace FoodStreetGuide.Services;

public interface ISettingsService
{
    // Geofence Settings
    double GetGeofenceRadius();
    void SetGeofenceRadius(double radius);
    
    // Cooldown Settings (in minutes)
    int GetCooldownMinutes();
    void SetCooldownMinutes(int minutes);
    
    // Narration Toggle
    bool GetNarrationEnabled();
    void SetNarrationEnabled(bool enabled);
    
    // GPS Settings
    int GetGPSIntervalMs();
    void SetGPSIntervalMs(int intervalMs);
    int GetGPSAccuracy();
    void SetGPSAccuracy(int accuracy);
    
    // TTS Settings
    string GetTTSVoice();
    void SetTTSVoice(string voice);
    
    float GetTTSRate();
    void SetTTSRate(float rate);
    
    // Reset to defaults
    void ResetToDefaults();
}

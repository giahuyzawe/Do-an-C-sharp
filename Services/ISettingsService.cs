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
    
    // Language Setting
    string GetLanguage(); // "vi" or "en"
    void SetLanguage(string language);
    
    // Auto Sync from Web Admin
    bool GetAutoSyncFromWeb();
    void SetAutoSyncFromWeb(bool enabled);
    
    // Geofence Trigger Mode: "sticky" (default), "nearest", "smart"
    // - sticky: Must exit POI before triggering another
    // - nearest: Always trigger nearest POI regardless of previous
    // - smart: Cooldown per POI, always trigger nearest available
    string GetGeofenceMode();
    void SetGeofenceMode(string mode);
    
    // Reset to defaults
    void ResetToDefaults();
}

// Geofence Mode Constants
public static class GeofenceModes
{
    public const string Sticky = "sticky";      // Must exit before triggering new POI
    public const string Nearest = "nearest";    // Always trigger nearest
    public const string Smart = "smart";        // Cooldown per POI + nearest priority
}

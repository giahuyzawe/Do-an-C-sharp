using FoodStreetGuide.Services;
using FoodStreetGuide.Platforms.Android;

namespace FoodStreetGuide;

public partial class SettingsPage : ContentPage
{
    private readonly ISettingsService? _settingsService;
    private readonly ITTSService? _ttsService;
    private readonly IGeofenceEngine? _geofenceEngine;
    private readonly ILocalizationService? _localizationService;

    public SettingsPage()
    {
        InitializeComponent();
        
        _settingsService = ServiceProviderHelper.GetService<ISettingsService>();
        _ttsService = ServiceProviderHelper.GetService<ITTSService>();
        _geofenceEngine = ServiceProviderHelper.GetService<IGeofenceEngine>();
        _localizationService = ServiceProviderHelper.GetService<ILocalizationService>();
        
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (_settingsService == null) return;

        double radius = _settingsService.GetGeofenceRadius();
        radiusSlider.Value = radius;
        radiusValueLabel.Text = $"{radius:F0}m";

        int cooldown = _settingsService.GetCooldownMinutes();
        cooldownSlider.Value = cooldown;
        cooldownValueLabel.Text = $"{cooldown} min";

        bool narration = _settingsService.GetNarrationEnabled();
        narrationSwitch.IsToggled = narration;

        int interval = _settingsService.GetGPSIntervalMs();
        intervalSlider.Value = interval / 1000;
        intervalValueLabel.Text = $"{interval / 1000} sec";

        string voice = _settingsService.GetTTSVoice();
        voicePicker.SelectedIndex = voice.StartsWith("en") ? 1 : 0;

        float rate = _settingsService.GetTTSRate();
        speedSlider.Value = rate;
        speedValueLabel.Text = $"{rate:F1}x";
    }

    private void OnRadiusChanged(object sender, ValueChangedEventArgs e)
    {
        radiusValueLabel.Text = $"{e.NewValue:F0}m";
    }

    private void OnCooldownChanged(object sender, ValueChangedEventArgs e)
    {
        cooldownValueLabel.Text = $"{e.NewValue:F0} min";
    }

    private void OnNarrationToggled(object sender, ToggledEventArgs e)
    {
        if (_settingsService != null)
        {
            _settingsService.SetNarrationEnabled(e.Value);
        }
    }

    private void OnVoiceChanged(object sender, EventArgs e)
    {
        if (voicePicker == null || _settingsService == null || _localizationService == null) return;
        
        // 0 = Vietnamese, 1 = English
        string language = voicePicker.SelectedIndex == 0 ? "vi" : "en";
        _localizationService.SetLanguage(language);
        
        // Update UI immediately
        LoadLocalizedUI();
    }
    
    private void LoadLocalizedUI()
    {
        if (_localizationService == null) return;
        var loc = _localizationService;
        
        // Update all UI elements with localized strings
        Title = loc.GetString("Settings_Title");
        
        // Profile section
        if (profileNameLabel != null) profileNameLabel.Text = loc.GetString("Settings_Profile");
        if (profileSubtitleLabel != null) profileSubtitleLabel.Text = loc.GetString("Settings_ProfileSubtitle");
        
        // Geofence section
        if (geofenceSectionLabel != null) geofenceSectionLabel.Text = loc.GetString("Settings_GeofenceSection");
        if (radiusLabel != null) radiusLabel.Text = loc.GetString("Settings_Radius");
        if (cooldownLabel != null) cooldownLabel.Text = loc.GetString("Settings_Cooldown");
        
        // Narration section
        if (narrationSectionLabel != null) narrationSectionLabel.Text = loc.GetString("Settings_NarrationSection");
        if (enableNarrationLabel != null) enableNarrationLabel.Text = loc.GetString("Settings_EnableNarration");
        if (languageLabel != null) languageLabel.Text = loc.GetString("Settings_Language");
        if (speedLabel != null) speedLabel.Text = loc.GetString("Settings_SpeechSpeed");
        
        // GPS section
        if (gpsSectionLabel != null) gpsSectionLabel.Text = loc.GetString("Settings_GPSSection");
        if (intervalLabel != null) intervalLabel.Text = loc.GetString("Settings_UpdateInterval");
        
        // About section
        if (aboutSectionLabel != null) aboutSectionLabel.Text = loc.GetString("Settings_AboutSection");
        if (versionLabel != null) versionLabel.Text = loc.GetString("Settings_Version");
        if (developerLabel != null) developerLabel.Text = loc.GetString("Settings_Developer");
        
        // Update unit labels
        if (cooldownValueLabel != null)
        {
            int cooldown = _settingsService?.GetCooldownMinutes() ?? 5;
            cooldownValueLabel.Text = string.Format(loc.GetString("Settings_Minutes"), cooldown);
        }
        if (intervalValueLabel != null)
        {
            int interval = _settingsService?.GetGPSIntervalMs() ?? 3000;
            intervalValueLabel.Text = string.Format(loc.GetString("Settings_Seconds"), interval / 1000);
        }
    }

    private void OnSpeedChanged(object sender, ValueChangedEventArgs e)
    {
        speedValueLabel.Text = $"{e.NewValue:F1}x";
    }

    private void OnIntervalChanged(object sender, ValueChangedEventArgs e)
    {
        intervalValueLabel.Text = $"{e.NewValue:F0} sec";
    }

}

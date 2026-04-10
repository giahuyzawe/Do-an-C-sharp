using FoodStreetGuide.Services;
using FoodStreetGuide.Platforms.Android;

namespace FoodStreetGuide;

public partial class SettingsPage : ContentPage
{
    private readonly ISettingsService? _settingsService;
    private readonly ITTSService? _ttsService;
    private readonly IGeofenceEngine? _geofenceEngine;

    public SettingsPage()
    {
        InitializeComponent();
        
        _settingsService = ServiceProviderHelper.GetService<ISettingsService>();
        _ttsService = ServiceProviderHelper.GetService<ITTSService>();
        _geofenceEngine = ServiceProviderHelper.GetService<IGeofenceEngine>();
        
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
    }

    private void OnVoiceChanged(object sender, EventArgs e)
    {
    }

    private void OnSpeedChanged(object sender, ValueChangedEventArgs e)
    {
        speedValueLabel.Text = $"{e.NewValue:F1}x";
    }

    private void OnIntervalChanged(object sender, ValueChangedEventArgs e)
    {
        intervalValueLabel.Text = $"{e.NewValue:F0} sec";
    }

    private async void OnSyncButtonClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Sync", "Syncing data from server...", "OK");
    }
}

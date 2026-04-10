using FoodStreetGuide.Models;
using FoodStreetGuide.Services;

namespace FoodStreetGuide;

public partial class POIListPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ITTSService? _ttsService;
    private readonly IAudioPlayerService? _audioPlayer;
    private readonly IWebAdminService? _webAdminService;

    public POIListPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _ttsService = ServiceProviderHelper.GetService<ITTSService>();
        _audioPlayer = ServiceProviderHelper.GetService<IAudioPlayerService>();
        _webAdminService = ServiceProviderHelper.GetService<IWebAdminService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPOIs();
    }

    private async Task LoadPOIs()
    {
        try
        {
            var pois = await _databaseService.GetPOIsAsync();
            poiCollectionView.ItemsSource = pois.OrderByDescending(p => p.Priority);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể tải danh sách: {ex.Message}", "OK");
        }
    }

    private async void OnAddPOIClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddPOIPage());
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        if (_webAdminService == null)
        {
            await DisplayAlert("Lỗi", "Không thể kết nối Web Admin", "OK");
            return;
        }

        var loading = new ActivityIndicator { IsRunning = true };
        await DisplayAlert("Đồng bộ", "Đang đồng bộ dữ liệu...", null);

        try
        {
            await _webAdminService.SyncFromWebAdminAsync();
            await LoadPOIs();
            await DisplayAlert("Thành công", "Đã đồng bộ POI từ Web Admin!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể đồng bộ: {ex.Message}", "OK");
        }
    }

    private async void OnPOISelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is POI selectedPOI)
        {
            // Deselect
            ((CollectionView)sender).SelectedItem = null;
            
            var actions = new List<string> { "Xem trên bản đồ (App)", "Mở Google Maps" };
            
            if (_ttsService != null)
                actions.Add("🔊 Đọc mô tả (TTS)");
            
            if (_audioPlayer != null && !string.IsNullOrEmpty(selectedPOI.AudioVi))
                actions.Add("🎵 Phát audio Tiếng Việt");
            
            if (_audioPlayer != null && !string.IsNullOrEmpty(selectedPOI.AudioEn))
                actions.Add("🎵 Phát audio English");
            
            actions.Add("Chỉ đường tới đây");

            var action = await DisplayActionSheet(
                selectedPOI.NameVi, 
                "Hủy", 
                "Xóa",
                actions.ToArray());

            await HandleAction(action, selectedPOI);
        }
    }

    private async Task HandleAction(string action, POI poi)
    {
        switch (action)
        {
            case "Xem trên bản đồ (App)":
                await Navigation.PushAsync(new MainPage());
                break;
                
            case "Mở Google Maps":
                await Launcher.OpenAsync(poi.GetGoogleMapsUrl());
                break;
                
            case "🔊 Đọc mô tả (TTS)":
                if (_ttsService != null)
                {
                    var textToSpeak = !string.IsNullOrEmpty(poi.DescriptionVi) 
                        ? poi.DescriptionVi 
                        : poi.NameVi;
                    await _ttsService.SpeakAsync(textToSpeak, "vi-VN");
                }
                break;
                
            case "🎵 Phát audio Tiếng Việt":
                if (_audioPlayer != null && !string.IsNullOrEmpty(poi.AudioVi))
                    await _audioPlayer.PlayAsync(poi.AudioVi);
                break;
                
            case "🎵 Phát audio English":
                if (_audioPlayer != null && !string.IsNullOrEmpty(poi.AudioEn))
                    await _audioPlayer.PlayAsync(poi.AudioEn);
                break;
                
            case "Chỉ đường tới đây":
                var directionsUrl = $"https://www.google.com/maps/dir/?api=1&destination={poi.Latitude},{poi.Longitude}";
                await Launcher.OpenAsync(directionsUrl);
                break;
                
            case "Xóa":
                await DeletePOI(poi);
                break;
        }
    }

    private async Task DeletePOI(POI poi)
    {
        bool confirm = await DisplayAlert(
            "Xác nhận xóa", 
            $"Bạn có chắc muốn xóa \"{poi.NameVi}\"?", 
            "Xóa", "Hủy");

        if (confirm)
        {
            try
            {
                await _databaseService.DeletePOIAsync(poi);
                await LoadPOIs();
                await DisplayAlert("Thành công", "Đã xóa quán ăn", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Không thể xóa: {ex.Message}", "OK");
            }
        }
    }
}

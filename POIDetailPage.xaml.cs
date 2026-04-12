using FoodStreetGuide.Models;
using FoodStreetGuide.Services;

namespace FoodStreetGuide;

public partial class POIDetailPage : ContentPage
{
    private POI? _poi;
    private readonly ILocalizationService? _localizationService;
    private readonly ISettingsService? _settingsService;
    private readonly ISavedPOIService? _savedPOIService;

    public POIDetailPage(POI poi)
    {
        try
        {
            InitializeComponent();
            _poi = poi;
            _localizationService = ServiceProviderHelper.GetService<ILocalizationService>();
            _settingsService = ServiceProviderHelper.GetService<ISettingsService>();
            _savedPOIService = ServiceProviderHelper.GetService<ISavedPOIService>();
            
            // Update save button state based on service
            UpdateSaveButtonState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage.Constructor] Error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            // Delay to ensure UI is ready
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50);
                LoadPOIData();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage.OnAppearing] Error: {ex.Message}");
        }
    }

    private void LoadPOIData()
    {
        try
        {
            if (_poi == null) return;
            
            var language = _settingsService?.GetLanguage() ?? "vi";
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Basic info
                    if (titleLabel != null) titleLabel.Text = language == "en" ? "Restaurant Details" : "Chi tiết nhà hàng";
                    if (poiNameLabel != null) poiNameLabel.Text = language == "en" ? _poi.NameEn : _poi.NameVi;
                    if (poiAddressLabel != null) poiAddressLabel.Text = $"📍 {_poi.Address}";
                    if (poiRatingLabel != null) poiRatingLabel.Text = "⭐ 4.5";
                    
                    // Status
                    UpdateStatus(language);
                    
                    // Hours
                    if (!string.IsNullOrEmpty(_poi.OpeningHours))
                    {
                        if (poiHoursLabel != null)
                        {
                            poiHoursLabel.Text = $"🕐 {_poi.OpeningHours}";
                            poiHoursLabel.IsVisible = true;
                        }
                    }
                    else
                    {
                        if (poiHoursLabel != null) poiHoursLabel.IsVisible = false;
                    }
                    
                    // Description
                    var description = language == "en" ? _poi.DescriptionEn : _poi.DescriptionVi;
                    if (poiDescriptionLabel != null) poiDescriptionLabel.Text = description ?? (language == "en" ? "No description available." : "Chưa có mô tả.");
                    if (descriptionTitleLabel != null) descriptionTitleLabel.Text = language == "en" ? "About" : "Giới thiệu";
                    if (dishesTitleLabel != null) dishesTitleLabel.Text = language == "en" ? "Featured Dishes" : "Món nổi bật";
                    
                    // Images
                    var images = GetPOIImages(_poi);
                    if (poiImageCarousel != null) poiImageCarousel.ItemsSource = images;
                    
                    // Load featured dishes
                    LoadFeaturedDishes(language);
                    
                    // Button labels
                    if (backButton != null) backButton.Text = "&#8592;";
                    if (directionsButton != null) directionsButton.Text = language == "en" ? "&#10148; Directions" : "&#10148; Chỉ đường";
                    if (callButton != null) callButton.Text = language == "en" ? "&#128222; Call" : "&#128222; Gọi điện";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadPOIData.UI] Error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadPOIData] Error: {ex.Message}");
        }
    }

    private void UpdateStatus(string language)
    {
        if (_poi == null || poiStatusLabel == null) return;
        
        bool isOpen = IsCurrentlyOpen(_poi.OpeningHours);
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (isOpen)
                {
                    poiStatusLabel.Text = language == "en" ? "Open" : "Đang mở cửa";
                    poiStatusLabel.TextColor = Color.FromArgb("#4CAF50");
                }
                else
                {
                    poiStatusLabel.Text = language == "en" ? "Closed" : "Đã đóng cửa";
                    poiStatusLabel.TextColor = Color.FromArgb("#F44336");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateStatus] Error: {ex.Message}");
            }
        });
    }

    private bool IsCurrentlyOpen(string? openingHours)
    {
        if (string.IsNullOrEmpty(openingHours)) return true;
        
        try
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            
            if (openingHours.Contains("-"))
            {
                var parts = openingHours.Split('-');
                if (parts.Length == 2 && 
                    TimeSpan.TryParse(parts[0].Trim(), out var openTime) &&
                    TimeSpan.TryParse(parts[1].Trim(), out var closeTime))
                {
                    if (closeTime < openTime)
                        return currentTime >= openTime || currentTime <= closeTime;
                    return currentTime >= openTime && currentTime <= closeTime;
                }
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    private List<string> GetPOIImages(POI poi)
    {
        var images = new List<string>();
        
        if (!string.IsNullOrEmpty(poi.Image))
            images.Add(poi.Image);
        else
            images.Add("restaurant_placeholder.png");
        
        // Add variation images
        string[] foodImages = { "pho.png", "banhmi.png", "bunbo.png", "comtam.png", "banhxeo.png" };
        var random = new Random(poi.Id);
        int additionalImages = random.Next(1, 4);
        
        for (int i = 0; i < additionalImages; i++)
            images.Add(foodImages[(poi.Id + i) % foodImages.Length]);
        
        return images;
    }

    private void LoadFeaturedDishes(string language)
    {
        if (dishesContainer == null) return;
        
        try
        {
            dishesContainer.Children.Clear();
            
            // Sample dishes based on POI name
            var dishes = GetSampleDishes(_poi?.NameVi ?? "", language);
            
            foreach (var dish in dishes)
        {
            var dishFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F7F7F7"),
                CornerRadius = 8,
                Padding = new Thickness(12),
                BorderColor = Color.FromArgb("#E3E3E3")
            };
            
            var dishLayout = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection(
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                )
            };
            
            var nameLabel = new Label
            {
                Text = dish.Name,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#2D2D2D"),
                VerticalOptions = LayoutOptions.Center
            };
            
            var priceLabel = new Label
            {
                Text = dish.Price,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#FF6B35"),
                VerticalOptions = LayoutOptions.Center
            };
            
            dishLayout.Children.Add(nameLabel);
            Grid.SetColumn(nameLabel, 0);
            dishLayout.Children.Add(priceLabel);
            Grid.SetColumn(priceLabel, 1);
            
            dishFrame.Content = dishLayout;
            dishesContainer.Children.Add(dishFrame);
        }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadFeaturedDishes] Error: {ex.Message}");
        }
    }

    private List<DishInfo> GetSampleDishes(string poiName, string language)
    {
        var dishes = new List<DishInfo>();
        
        if (poiName.Contains("Pho") || poiName.Contains("Phở"))
        {
            dishes.Add(new DishInfo { Name = language == "en" ? "Beef Pho" : "Phở bò", Price = "50,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Chicken Pho" : "Phở gà", Price = "45,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Rare Beef Pho" : "Phở tái", Price = "55,000đ" });
        }
        else if (poiName.Contains("Banh Mi") || poiName.Contains("Bánh Mì"))
        {
            dishes.Add(new DishInfo { Name = language == "en" ? "Mixed Banh Mi" : "Bánh mì thập cẩm", Price = "35,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Grilled Pork Banh Mi" : "Bánh mì thịt nướng", Price = "30,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Meatball Banh Mi" : "Bánh mì xíu mại", Price = "32,000đ" });
        }
        else if (poiName.Contains("Bun Bo") || poiName.Contains("Bún Bò"))
        {
            dishes.Add(new DishInfo { Name = language == "en" ? "Hue Beef Noodles" : "Bún bò Huế đặc biệt", Price = "60,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Beef Noodles (No Pork)" : "Bún bò giò heo", Price = "55,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Oxtail Noodles" : "Bún bò đuôi", Price = "70,000đ" });
        }
        else if (poiName.Contains("Com Tam") || poiName.Contains("Cơm Tấm"))
        {
            dishes.Add(new DishInfo { Name = language == "en" ? "Broken Rice with Pork Chop" : "Cơm tấm sườn nướng", Price = "45,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Shredded Pork Rice" : "Cơm tấm bì", Price = "40,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Mixed Rice" : "Cơm tấm đặc biệt", Price = "55,000đ" });
        }
        else
        {
            dishes.Add(new DishInfo { Name = language == "en" ? "Signature Dish" : "Món đặc sản", Price = "65,000đ" });
            dishes.Add(new DishInfo { Name = language == "en" ? "Special Combo" : "Combo đặc biệt", Price = "85,000đ" });
        }
        
        return dishes;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void UpdateSaveButtonState()
    {
        if (_poi == null || _savedPOIService == null || saveHeartLabel == null) return;
        
        bool isSaved = _savedPOIService.IsSaved(_poi);
        saveHeartLabel.TextColor = isSaved ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
        System.Diagnostics.Debug.WriteLine($"[POIDetailPage] Save button state: {(isSaved ? "saved" : "unsaved")}");
    }

    private void OnSaveTapped(object sender, TappedEventArgs e)
    {
        if (_poi == null || _savedPOIService == null) return;
        
        // Toggle save state using service
        _savedPOIService.ToggleSave(_poi);
        
        // Update UI
        UpdateSaveButtonState();
        
        var loc = _localizationService;
        var name = _poi?.NameVi ?? "";
        bool isNowSaved = _savedPOIService.IsSaved(_poi);
        DisplayAlert(
            loc?.GetString("Alert_Notification") ?? "Thông báo",
            isNowSaved ? $"❤️ Đã lưu {name}" : $"❤️ Đã bỏ lưu {name}",
            "OK");
        
        System.Diagnostics.Debug.WriteLine($"[OnSaveTapped] POI '{name}' is now {(isNowSaved ? "saved" : "unsaved")}");
    }

    private async void OnDirectionsClicked(object sender, EventArgs e)
    {
        if (_poi == null) return;
        
        try
        {
            var lang = _settingsService?.GetLanguage() ?? "vi";
            var name = lang == "en" ? _poi.NameEn : _poi.NameVi;
            var googleMapsUrl = $"https://www.google.com/maps/dir/?api=1&destination={_poi.Latitude},{_poi.Longitude}&destination_place_id={Uri.EscapeDataString(name)}";
            
            await Launcher.OpenAsync(new Uri(googleMapsUrl));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể mở bản đồ: {ex.Message}", "OK");
        }
    }

    private async void OnCallClicked(object sender, EventArgs e)
    {
        var phoneNumber = "+84123456789"; // Placeholder
        try
        {
            await Launcher.OpenAsync(new Uri($"tel:{phoneNumber}"));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể gọi điện: {ex.Message}", "OK");
        }
    }

    private class DishInfo
    {
        public string Name { get; set; } = "";
        public string Price { get; set; } = "";
    }
}

using FoodStreetGuide.Models;
using FoodStreetGuide.Services;

namespace FoodStreetGuide;

public partial class DiscoverPage : ContentPage
{
    private readonly ILocalizationService? _localizationService;
    private readonly ISettingsService? _settingsService;
    private readonly ILocationService? _locationService;
    private readonly ISavedPOIService? _savedPOIService;
    private List<POI> _allPOIs = new();
    private string _currentFilter = "popular"; // popular, distance, food

    // Sample restaurant data for demo
    private readonly List<RestaurantInfo> _restaurants = new()
    {
        new RestaurantInfo { Id = 1, NameVi = "Pho Hoa Pasteur", NameEn = "Pho Hoa Pasteur", 
            Distance = "500m", Rating = 4.8, Reviews = 215, OpeningHours = "06:00-22:00", 
            Status = "open", Type = "pho", Address = "260C Pasteur, Q.3, TP.HCM" },
        new RestaurantInfo { Id = 2, NameVi = "Banh Mi Huynh Hoa", NameEn = "Banh Mi Huynh Hoa", 
            Distance = "750m", Rating = 4.6, Reviews = 189, OpeningHours = "06:00-21:00", 
            Status = "open", Type = "banhmi", Address = "26 Lê Thị Riêng, Q.1, TP.HCM" },
        new RestaurantInfo { Id = 3, NameVi = "Com Tam Cali", NameEn = "Com Tam Cali", 
            Distance = "1.2km", Rating = 4.5, Reviews = 156, OpeningHours = "00:00-24:00", 
            Status = "open", Type = "comtam", Address = "36 Đỗ Quang Đẩu, Q.1, TP.HCM" },
        new RestaurantInfo { Id = 4, NameVi = "Bun Bo Hue", NameEn = "Bun Bo Hue", 
            Distance = "1.8km", Rating = 4.7, Reviews = 203, OpeningHours = "06:30-21:30", 
            Status = "closed", Type = "bunbo", Address = "110 Lý Tự Trọng, Q.1, TP.HCM" }
    };

    public DiscoverPage()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DiscoverPage] Constructor START");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[DiscoverPage] InitializeComponent DONE");
            _localizationService = ServiceProviderHelper.GetService<ILocalizationService>();
            _settingsService = ServiceProviderHelper.GetService<ISettingsService>();
            _locationService = ServiceProviderHelper.GetService<ILocationService>();
            _savedPOIService = ServiceProviderHelper.GetService<ISavedPOIService>();
            System.Diagnostics.Debug.WriteLine("[DiscoverPage] Constructor END");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DiscoverPage.Constructor] CRITICAL ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[DiscoverPage.Constructor] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            UpdateLanguage();
            UpdateFilterUI(); // Set initial filter state
            
            // Delay status update to ensure UI is ready
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                UpdateCardStatuses();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DiscoverPage.OnAppearing] Error: {ex.Message}");
        }
    }

    // Card Click Handlers - Open Detail Page
    private async void OnCard1Tapped(object? sender, TappedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DiscoverPage] Card 1 tapped");
            var poi = CreatePOIFromRestaurant(_restaurants[0]);
            await Navigation.PushAsync(new POIDetailPage(poi));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnCard1Tapped] Error: {ex.Message}");
            await DisplayAlert("Lỗi", $"Không thể mở chi tiết: {ex.Message}", "OK");
        }
    }

    private async void OnCard2Tapped(object? sender, TappedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DiscoverPage] Card 2 tapped");
            var poi = CreatePOIFromRestaurant(_restaurants[1]);
            await Navigation.PushAsync(new POIDetailPage(poi));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnCard2Tapped] Error: {ex.Message}");
            await DisplayAlert("Lỗi", $"Không thể mở chi tiết: {ex.Message}", "OK");
        }
    }

    private async void OnCard3Tapped(object? sender, TappedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DiscoverPage] Card 3 tapped");
            var poi = CreatePOIFromRestaurant(_restaurants[2]);
            await Navigation.PushAsync(new POIDetailPage(poi));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnCard3Tapped] Error: {ex.Message}");
            await DisplayAlert("Lỗi", $"Không thể mở chi tiết: {ex.Message}", "OK");
        }
    }

    private async void OnCard4Tapped(object? sender, TappedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[DiscoverPage] Card 4 tapped");
            var poi = CreatePOIFromRestaurant(_restaurants[3]);
            await Navigation.PushAsync(new POIDetailPage(poi));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnCard4Tapped] Error: {ex.Message}");
            await DisplayAlert("Lỗi", $"Không thể mở chi tiết: {ex.Message}", "OK");
        }
    }

    private POI CreatePOIFromRestaurant(RestaurantInfo r)
    {
        return new POI
        {
            Id = r.Id,
            NameVi = r.NameVi,
            NameEn = r.NameEn,
            DescriptionVi = $"Nhà hàng {r.NameVi} nổi tiếng với món ăn ngon và giá cả hợp lý.",
            DescriptionEn = $"{r.NameEn} is famous for delicious food at affordable prices.",
            Address = r.Address,
            OpeningHours = r.OpeningHours,
            Latitude = 10.7769 + (r.Id * 0.001), // Sample coordinates
            Longitude = 106.7009 + (r.Id * 0.001),
            Radius = 100,
            Priority = 1
        };
    }

    // Filter Handlers
    private void OnFilterPopularTapped(object? sender, TappedEventArgs e)
    {
        _currentFilter = "popular";
        UpdateFilterUI();
        // Sort by rating
        _restaurants.Sort((a, b) => b.Rating.CompareTo(a.Rating));
        UpdateCardOrder();
    }

    private async void OnFilterDistanceTapped(object? sender, TappedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[OnFilterDistanceTapped] Button clicked");
        
        _currentFilter = "distance";
        UpdateFilterUI();
        
        // Get current location and calculate real distances
        await UpdateDistancesFromCurrentLocation();
        
        // Sort by actual distance
        _restaurants.Sort((a, b) => ParseDistance(a.Distance).CompareTo(ParseDistance(b.Distance)));
        
        System.Diagnostics.Debug.WriteLine("[OnFilterDistanceTapped] After sort:");
        foreach (var r in _restaurants)
        {
            System.Diagnostics.Debug.WriteLine($"  - {r.NameVi}: {r.Distance}");
        }
        
        UpdateCardOrder();
    }

    private async Task UpdateDistancesFromCurrentLocation()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[UpdateDistancesFromCurrentLocation] Starting...");
            
            if (_locationService == null) 
            {
                System.Diagnostics.Debug.WriteLine("[UpdateDistances] Location service is null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("[UpdateDistances] Getting current location...");
            var currentLocation = await _locationService.GetCurrentLocationAsync();
            
            if (currentLocation == null) 
            {
                System.Diagnostics.Debug.WriteLine("[UpdateDistances] Current location is null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[UpdateDistances] Got location: {currentLocation.Latitude}, {currentLocation.Longitude}");
            
            // Update distances based on actual coordinates
            foreach (var r in _restaurants)
            {
                var poi = CreatePOIFromRestaurant(r);
                var distance = CalculateDistance(currentLocation.Latitude, currentLocation.Longitude, 
                    poi.Latitude, poi.Longitude);
                
                var oldDistance = r.Distance;
                
                // Format distance
                if (distance < 1000)
                    r.Distance = $"{distance:F0}m";
                else
                    r.Distance = $"{distance / 1000:F1}km";
                
                System.Diagnostics.Debug.WriteLine($"[UpdateDistances] {r.NameVi}: {oldDistance} -> {r.Distance} ({distance:F0}m)");
            }
            
            // Update UI
            UpdateCardDistances();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateDistances] Error: {ex.Message}");
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double R = 6371000; // Earth radius in meters
        var latRad1 = lat1 * Math.PI / 180;
        var latRad2 = lat2 * Math.PI / 180;
        var deltaLat = (lat2 - lat1) * Math.PI / 180;
        var deltaLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(latRad1) * Math.Cos(latRad2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private double ParseDistance(string distance)
    {
        if (string.IsNullOrEmpty(distance)) return double.MaxValue;
        
        distance = distance.Trim().ToLower();
        if (distance.EndsWith("m") && !distance.EndsWith("km"))
        {
            if (double.TryParse(distance.Replace("m", "").Trim(), out var m))
                return m;
        }
        else if (distance.EndsWith("km"))
        {
            if (double.TryParse(distance.Replace("km", "").Trim(), out var km))
                return km * 1000;
        }
        
        return double.MaxValue;
    }

    private void UpdateCardDistances()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (card1DistanceLabel != null) card1DistanceLabel.Text = _restaurants[0].Distance;
                if (card2DistanceLabel != null) card2DistanceLabel.Text = _restaurants[1].Distance;
                if (card3DistanceLabel != null) card3DistanceLabel.Text = _restaurants[2].Distance;
                if (card4DistanceLabel != null) card4DistanceLabel.Text = _restaurants[3].Distance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCardDistances] Error: {ex.Message}");
            }
        });
    }

    private void OnFilterFoodTapped(object? sender, TappedEventArgs e)
    {
        _currentFilter = "food";
        UpdateFilterUI();
        // Show only Vietnamese food (all in this case)
        UpdateCardOrder();
    }

    private void UpdateFilterUI()
    {
        if (filterPopularFrame == null || filterDistanceFrame == null || filterFoodFrame == null) return;
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Reset all filters to inactive
                filterPopularFrame.BackgroundColor = Color.FromArgb("#FFFFFF");
                filterPopularFrame.BorderColor = Color.FromArgb("#E3E3E3");
                if (filterPopularLabel != null) filterPopularLabel.TextColor = Color.FromArgb("#2D2D2D");
                
                filterDistanceFrame.BackgroundColor = Color.FromArgb("#FFFFFF");
                filterDistanceFrame.BorderColor = Color.FromArgb("#E3E3E3");
                if (filterDistanceLabel != null) filterDistanceLabel.TextColor = Color.FromArgb("#2D2D2D");
                
                filterFoodFrame.BackgroundColor = Color.FromArgb("#FFFFFF");
                filterFoodFrame.BorderColor = Color.FromArgb("#E3E3E3");
                if (filterFoodLabel != null) filterFoodLabel.TextColor = Color.FromArgb("#2D2D2D");
                
                // Set active filter
                switch (_currentFilter)
                {
                    case "popular":
                        filterPopularFrame.BackgroundColor = Color.FromArgb("#FF6B35");
                        filterPopularFrame.BorderColor = Color.FromArgb("#FF6B35");
                        if (filterPopularLabel != null) filterPopularLabel.TextColor = Color.FromArgb("#FFFFFF");
                        break;
                    case "distance":
                        filterDistanceFrame.BackgroundColor = Color.FromArgb("#FF6B35");
                        filterDistanceFrame.BorderColor = Color.FromArgb("#FF6B35");
                        if (filterDistanceLabel != null) filterDistanceLabel.TextColor = Color.FromArgb("#FFFFFF");
                        break;
                    case "food":
                        filterFoodFrame.BackgroundColor = Color.FromArgb("#FF6B35");
                        filterFoodFrame.BorderColor = Color.FromArgb("#FF6B35");
                        if (filterFoodLabel != null) filterFoodLabel.TextColor = Color.FromArgb("#FFFFFF");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateFilterUI] Error: {ex.Message}");
            }
        });
    }

    // Store original card references for reordering
    private readonly Dictionary<int, Frame> _cardFrames = new();
    private bool _cardsInitialized = false;

    private void InitializeCardReferences()
    {
        if (_cardsInitialized) return;
        
        // Store references to card frames
        _cardFrames[0] = card1;
        _cardFrames[1] = card2;
        _cardFrames[2] = card3;
        _cardFrames[3] = card4;
        
        _cardsInitialized = true;
    }

    private void UpdateCardOrder()
    {
        try
        {
            InitializeCardReferences();
            
            // Update all card content based on sorted _restaurants order
            for (int i = 0; i < _restaurants.Count && i < 4; i++)
            {
                UpdateCardContent(i, _restaurants[i]);
            }
            
            System.Diagnostics.Debug.WriteLine("[UpdateCardOrder] Cards reordered");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateCardOrder] Error: {ex.Message}");
        }
    }

    private void UpdateCardContent(int cardIndex, RestaurantInfo r)
    {
        // Update name label directly by x:Name
        var nameLabels = new[] { card1NameLabel, card2NameLabel, card3NameLabel, card4NameLabel };
        if (cardIndex < nameLabels.Length && nameLabels[cardIndex] != null)
            nameLabels[cardIndex].Text = r.NameVi;
        
        // Update distance
        var distanceLabels = new[] { card1DistanceLabel, card2DistanceLabel, card3DistanceLabel, card4DistanceLabel };
        if (cardIndex < distanceLabels.Length && distanceLabels[cardIndex] != null)
            distanceLabels[cardIndex].Text = r.Distance;
        
        // Update status
        var statusLabels = new[] { card1StatusLabel, card2StatusLabel, card3StatusLabel, card4StatusLabel };
        if (cardIndex < statusLabels.Length && statusLabels[cardIndex] != null)
        {
            var lang = _settingsService?.GetLanguage() ?? "vi";
            bool isOpen = IsCurrentlyOpen(r.OpeningHours);
            statusLabels[cardIndex].Text = isOpen 
                ? (lang == "en" ? "Open" : "Đang mở") 
                : (lang == "en" ? "Closed" : "Đã đóng");
            statusLabels[cardIndex].TextColor = isOpen 
                ? Color.FromArgb("#4CAF50") 
                : Color.FromArgb("#F44336");
        }
        
        // Update save button ClassId to identify which restaurant
        var saveButtons = new[] { card1SaveButton, card2SaveButton, card3SaveButton, card4SaveButton };
        if (cardIndex < saveButtons.Length && saveButtons[cardIndex] != null)
        {
            saveButtons[cardIndex].ClassId = r.Id.ToString();
        }
        
        System.Diagnostics.Debug.WriteLine($"[UpdateCardContent] Card {cardIndex} -> {r.NameVi} ({r.Distance})");
    }

    private Label? FindLabelInChildren(VerticalStackLayout layout, Func<Label, bool> predicate)
    {
        foreach (var child in layout.Children)
        {
            if (child is Label label && predicate(label))
                return label;
            
            if (child is HorizontalStackLayout hLayout)
            {
                foreach (var hChild in hLayout.Children)
                {
                    if (hChild is Label hLabel && predicate(hLabel))
                        return hLabel;
                }
            }
            
            if (child is Grid grid)
            {
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is Label gLabel && predicate(gLabel))
                        return gLabel;
                    
                    if (gridChild is VerticalStackLayout vLayout)
                    {
                        foreach (var vChild in vLayout.Children)
                        {
                            if (vChild is Label vLabel && predicate(vLabel))
                                return vLabel;
                            
                            if (vChild is HorizontalStackLayout hvLayout)
                            {
                                foreach (var hvChild in hvLayout.Children)
                                {
                                    if (hvChild is Label hvLabel && predicate(hvLabel))
                                        return hvLabel;
                                }
                            }
                        }
                    }
                }
            }
        }
        return null;
    }

    private Label? FindLabelByPattern(VisualElement parent, string pattern)
    {
        return FindLabelInChildren((VerticalStackLayout)parent, l => l.Text?.Contains(pattern) == true);
    }

    private void UpdateCardStatuses()
    {
        try
        {
            var language = _settingsService?.GetLanguage() ?? "vi";
            
            // Update each card's status
            UpdateCardStatus(card1StatusLabel, _restaurants[0], language);
            UpdateCardStatus(card2StatusLabel, _restaurants[1], language);
            UpdateCardStatus(card3StatusLabel, _restaurants[2], language);
            UpdateCardStatus(card4StatusLabel, _restaurants[3], language);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateCardStatuses] Error: {ex.Message}");
        }
    }

    private void UpdateCardStatus(Label? statusLabel, RestaurantInfo r, string language)
    {
        if (statusLabel == null) return;
        
        bool isOpen = IsCurrentlyOpen(r.OpeningHours);
        
        if (isOpen)
        {
            statusLabel.Text = language == "en" ? "Open" : "Đang mở";
            statusLabel.TextColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            statusLabel.Text = language == "en" ? "Closed" : "Đã đóng";
            statusLabel.TextColor = Color.FromArgb("#F44336");
        }
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
            else if (openingHours.Contains("24"))
            {
                return true; // Open 24h
            }
            
            return true;
        }
        catch
        {
            return true;
        }
    }

    private void OnSaveTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Frame frame)
        {
            var label = frame.FindByName<Label>("saveLabel") ?? 
                       frame.Content as Label;
            if (label != null)
            {
                // Get restaurant ID from ClassId
                var restaurantId = int.Parse(frame.ClassId ?? "0");
                var restaurant = _restaurants.FirstOrDefault(r => r.Id == restaurantId);
                
                if (restaurant == null || _savedPOIService == null) return;
                
                // Create POI from restaurant
                var poi = CreatePOIFromRestaurant(restaurant);
                
                // Check current state
                bool wasSaved = _savedPOIService.IsSaved(poi);
                
                // Toggle save
                _savedPOIService.ToggleSave(poi);
                
                // Update UI
                bool isNowSaved = !wasSaved;
                label.TextColor = isNowSaved ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
                
                var loc = _localizationService;
                DisplayAlert(
                    loc?.GetString("Alert_Notification") ?? "Thông báo",
                    isNowSaved ? $"Đã lưu {restaurant.NameVi}" : $"Đã bỏ lưu {restaurant.NameVi}",
                    "OK");
                
                System.Diagnostics.Debug.WriteLine($"[OnSaveTapped] {restaurant.NameVi} is now {(isNowSaved ? "saved" : "unsaved")}");
            }
        }
    }

    private void UpdateLanguage()
    {
        try
        {
            if (_localizationService == null) return;
            var loc = _localizationService;
            
            Title = loc.GetString("Tab_Discover");
            if (pageTitleLabel != null) pageTitleLabel.Text = loc.GetString("Discover_Title");
            if (pageSubtitleLabel != null) pageSubtitleLabel.Text = loc.GetString("Discover_Subtitle");
            
            if (filterPopularLabel != null) filterPopularLabel.Text = loc.GetString("Filter_Popular");
            if (filterDistanceLabel != null) filterDistanceLabel.Text = loc.GetString("Filter_Distance");
            if (filterFoodLabel != null) filterFoodLabel.Text = loc.GetString("Filter_Food");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateLanguage] Error: {ex.Message}");
        }
    }

    private class RestaurantInfo
    {
        public int Id { get; set; }
        public string NameVi { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string Distance { get; set; } = "";
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public string OpeningHours { get; set; } = "";
        public string Status { get; set; } = "";
        public string Type { get; set; } = "";
        public string Address { get; set; } = "";
    }
}

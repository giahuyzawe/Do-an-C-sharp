using FoodStreetGuide.Models;
using FoodStreetGuide.Services;
using System.Linq;

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
            // Track POI view for analytics (local + API)
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_poi != null)
                    {
                        var deviceId = Preferences.Get("DeviceId", string.Empty);
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            // Local record
                            await App.Database.RecordPOIViewAsync(_poi.Id, deviceId, "detail_page");
                            
                            // Send to Web Admin API
                            var apiService = new ApiService();
                            var result = await apiService.PostAnalyticsAsync("poi_view", deviceId, _poi.Id);
                            if (result.Success)
                            {
                                System.Diagnostics.Debug.WriteLine($"[POIDetailPage] POI view sent to Web Admin: {_poi.Id}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[POIDetailPage] Failed to record view: {ex.Message}");
                }
            });
            
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
            if (_poi == null) 
            {
                System.Diagnostics.Debug.WriteLine("[LoadPOIData] ERROR: _poi is null");
                return;
            }
            
            // Debug: Log POI data
            System.Diagnostics.Debug.WriteLine($"[LoadPOIData] POI ID: {_poi.Id}, Name: {_poi.NameVi}");
            System.Diagnostics.Debug.WriteLine($"[LoadPOIData] Address: '{_poi.Address}'");
            System.Diagnostics.Debug.WriteLine($"[LoadPOIData] OpeningHours: '{_poi.OpeningHours}'");
            System.Diagnostics.Debug.WriteLine($"[LoadPOIData] DescriptionVi: '{_poi.DescriptionVi}'");
            
            var language = _settingsService?.GetLanguage() ?? "vi";
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Basic info
                    if (titleLabel != null) titleLabel.Text = language == "en" ? "Restaurant Details" : "Chi tiết nhà hàng";
                    if (poiNameLabel != null) poiNameLabel.Text = language == "en" ? _poi.NameEn : _poi.NameVi;
                    
                    // Address - fix null handling
                    var addressText = string.IsNullOrWhiteSpace(_poi.Address) 
                        ? (language == "en" ? "📍 Address not available" : "📍 Chưa có địa chỉ")
                        : $"📍 {_poi.Address}";
                    if (poiAddressLabel != null) 
                    {
                        poiAddressLabel.Text = addressText;
                        poiAddressLabel.IsVisible = true;
                    }
                    
                    if (poiRatingLabel != null) poiRatingLabel.Text = "⭐ 4.5";
                    
                    // Status
                    UpdateStatus(language);
                    
                    // Hours - fix null handling
                    if (!string.IsNullOrWhiteSpace(_poi.OpeningHours))
                    {
                        if (poiHoursLabel != null)
                        {
                            poiHoursLabel.Text = $"🕐 {_poi.OpeningHours}";
                            poiHoursLabel.IsVisible = true;
                        }
                    }
                    else
                    {
                        if (poiHoursLabel != null) 
                        {
                            poiHoursLabel.Text = language == "en" ? "🕐 Hours not available" : "🕐 Chưa có giờ mở";
                            poiHoursLabel.IsVisible = true;
                        }
                    }
                    
                    // Description - fix null handling
                    var description = language == "en" ? _poi.DescriptionEn : _poi.DescriptionVi;
                    var descText = string.IsNullOrWhiteSpace(description) 
                        ? (language == "en" ? "No description available." : "Chưa có mô tả.")
                        : description;
                    if (poiDescriptionLabel != null) poiDescriptionLabel.Text = descText;
                    if (descriptionTitleLabel != null) descriptionTitleLabel.Text = language == "en" ? "About" : "Giới thiệu";
                    if (dishesTitleLabel != null) dishesTitleLabel.Text = language == "en" ? "Featured Dishes" : "Món nổi bật";
                    
                    // Images
                    var images = GetPOIImages(_poi);
                    if (poiImageCarousel != null) poiImageCarousel.ItemsSource = images;
                    
                    // Featured dishes
                    LoadFeaturedDishes(language);
                    
                    // Reviews
                    _ = LoadReviewsAsync();
                    
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

    // ==================== REVIEWS ====================
    private int _selectedRating = 0;
    private bool _isReviewFormVisible = false;

    private async Task LoadReviewsAsync()
    {
        try
        {
            if (_poi == null) 
            {
                System.Diagnostics.Debug.WriteLine("[POIDetailPage.LoadReviewsAsync] ERROR: _poi is null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] START loading reviews for POI {_poi.Id}");
            
            // First load local reviews
            var localReviews = await App.Database.GetReviewsAsync(_poi.Id);
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Found {localReviews.Count} local reviews");
            
            // Then try to fetch from Web Admin
            try
            {
                var apiService = new ApiService();
                var webResult = await apiService.GetReviewsAsync(_poi.Id);
                
                System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Web API result: Success={webResult.Success}, Count={webResult.Data?.Count ?? 0}");
                
                if (webResult.Success && webResult.Data?.Data != null)
                {
                    var webReviews = webResult.Data.Data;
                    System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Fetched {webReviews.Length} reviews from Web Admin");
                    
                    // If web returns empty, delete ALL local reviews for this POI
                    if (webReviews.Length == 0 && localReviews.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Web returned empty, deleting {localReviews.Count} local reviews");
                        foreach (var localReview in localReviews)
                        {
                            await App.Database.DeleteReviewAsync(localReview);
                        }
                        localReviews.Clear();
                    }
                    else if (webReviews.Length > 0)
                    {
                        var webReviewIds = webReviews.Select(r => r.Id).ToList();
                        System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Web review IDs: {string.Join(", ", webReviewIds)}");
                        
                        // DELETE reviews that were removed from Web Admin
                        var reviewsToDelete = localReviews.Where(r => !string.IsNullOrEmpty(r.WebReviewId) && !webReviewIds.Contains(r.WebReviewId)).ToList();
                        System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Need to delete {reviewsToDelete.Count} local reviews");
                        
                        foreach (var localReview in reviewsToDelete)
                        {
                            await App.Database.DeleteReviewByWebIdAsync(localReview.WebReviewId);
                            System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Deleted review removed from Web: {localReview.WebReviewId}");
                        }
                        
                        // ADD new reviews from Web
                        foreach (var webReview in webReviews)
                        {
                            var exists = localReviews.Any(r => r.WebReviewId == webReview.Id);
                            if (!exists)
                            {
                                var newReview = new Review
                                {
                                    POIId = webReview.PoiId,
                                    UserId = webReview.UserId ?? "",
                                    UserName = webReview.UserName ?? "Khách tham quan",
                                    Rating = webReview.Rating,
                                    Comment = webReview.Comment ?? "",
                                    CreatedAt = DateTime.TryParse(webReview.CreatedAt, out var date) ? date : DateTime.Now,
                                    WebReviewId = webReview.Id,
                                    LastSyncFromWeb = DateTime.Now
                                };
                                
                                await App.Database.AddReviewAsync(newReview);
                                System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Added web review: {webReview.Id}, Rating={webReview.Rating}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Review already exists: {webReview.Id}");
                            }
                        }
                        
                        // Reload after sync
                        localReviews = await App.Database.GetReviewsAsync(_poi.Id);
                        System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] After sync: {localReviews.Count} local reviews");
                    }
                }
                else if (!webResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Web API failed: {webResult.Error}");
                }
            }
            catch (Exception apiEx)
            {
                System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Failed to fetch web reviews: {apiEx.Message}");
            }
            
            var reviews = localReviews;
            var averageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
            
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Final: {reviews.Count} reviews, Avg={averageRating:F1}");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] UI Update - Rating={averageRating:F1}, Count={reviews.Count}");
                
                // Update average rating display
                if (averageRatingLabel != null) 
                    averageRatingLabel.Text = averageRating.ToString("F1");
                if (totalReviewsLabel != null) 
                    totalReviewsLabel.Text = $"({reviews.Count} đánh giá)";
                
                // Clear and populate reviews container
                if (reviewsContainer != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Rendering {reviews.Count} reviews to UI");
                    reviewsContainer.Children.Clear();
                    
                    foreach (var review in reviews.Take(5)) // Show max 5 reviews
                    {
                        System.Diagnostics.Debug.WriteLine($"[POIDetailPage.LoadReviewsAsync] Rendering review: {review.UserName}, Rating={review.Rating}");
                        var reviewFrame = new Frame
                        {
                            BackgroundColor = Color.FromArgb("#F7F7F7"),
                            CornerRadius = 8,
                            Padding = new Thickness(12),
                            BorderColor = Colors.Transparent
                        };
                        
                        var reviewLayout = new VerticalStackLayout { Spacing = 4 };
                        
                        // Stars
                        var stars = string.Join("", Enumerable.Repeat("⭐", review.Rating));
                        reviewLayout.Children.Add(new Label 
                        { 
                            Text = stars, 
                            FontSize = 12 
                        });
                        
                        // Comment
                        if (!string.IsNullOrEmpty(review.Comment))
                        {
                            reviewLayout.Children.Add(new Label 
                            { 
                                Text = review.Comment, 
                                FontSize = 14,
                                TextColor = Color.FromArgb("#2D2D2D"),
                                LineBreakMode = LineBreakMode.WordWrap
                            });
                        }
                        
                        // Date
                        reviewLayout.Children.Add(new Label 
                        { 
                            Text = review.CreatedAt.ToString("dd/MM/yyyy"), 
                            FontSize = 12,
                            TextColor = Color.FromArgb("#8E8E93")
                        });
                        
                        reviewFrame.Content = reviewLayout;
                        reviewsContainer.Children.Add(reviewFrame);
                    }
                    
                    // Add "See all" button if more than 5
                    if (reviews.Count > 5)
                    {
                        var seeAllLabel = new Label 
                        { 
                            Text = $"Xem thêm {reviews.Count - 5} đánh giá...",
                            FontSize = 14,
                            TextColor = Color.FromArgb("#FF6B35"),
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center
                        };
                        seeAllLabel.GestureRecognizers.Add(new TapGestureRecognizer
                        {
                            Command = new Command(async () => await DisplayAlert("Thông báo", "Tính năng xem tất cả đánh giá sẽ có trong phiên bản sau!", "OK"))
                        });
                        reviewsContainer.Children.Add(seeAllLabel);
                    }
                }
            });
            
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage] Loaded {reviews.Count} reviews");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage] Error loading reviews: {ex.Message}");
        }
    }

    private void OnAddReviewClicked(object? sender, EventArgs e)
    {
        _isReviewFormVisible = !_isReviewFormVisible;
        
        if (addReviewButton != null) 
            addReviewButton.IsVisible = !_isReviewFormVisible;
        if (reviewFormContainer != null) 
            reviewFormContainer.IsVisible = _isReviewFormVisible;
        
        // Reset form
        _selectedRating = 0;
        UpdateStarDisplay();
        if (reviewCommentEditor != null) 
            reviewCommentEditor.Text = "";
    }

    private void OnCancelReviewClicked(object? sender, EventArgs e)
    {
        _isReviewFormVisible = false;
        
        if (addReviewButton != null) 
            addReviewButton.IsVisible = true;
        if (reviewFormContainer != null) 
            reviewFormContainer.IsVisible = false;
    }

    private async void OnSubmitReviewClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_poi == null) return;
            if (_selectedRating == 0)
            {
                await DisplayAlert("Thông báo", "Vui lòng chọn số sao đánh giá!", "OK");
                return;
            }
            
            var deviceId = Preferences.Get("DeviceId", Guid.NewGuid().ToString());
            Preferences.Set("DeviceId", deviceId);
            
            var review = new Review
            {
                POIId = _poi.Id,
                Rating = _selectedRating,
                Comment = reviewCommentEditor?.Text ?? "",
                UserId = deviceId,
                UserName = "Khách tham quan", // Anonymous user
                CreatedAt = DateTime.Now
            };
            
            // Capture comment BEFORE clearing form
            var commentText = reviewCommentEditor?.Text ?? "";
            
            await App.Database.AddReviewAsync(review);
            
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage] Review added locally: {_selectedRating} stars");
            
            // Hide form and reload reviews immediately (local)
            OnCancelReviewClicked(sender, e);
            await LoadReviewsAsync();
            
            // Sync to Web Admin
            _ = Task.Run(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[POIDetailPage] Syncing review to Web Admin: POI={_poi.Id}, Rating={_selectedRating}");
                    var apiService = new ApiService();
                    var result = await apiService.PostReviewAsync(_poi.Id, deviceId, "Khách tham quan", _selectedRating, commentText);
                    if (result.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"[POIDetailPage] ✓ Review synced to Web Admin");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[POIDetailPage] ✗ Failed to sync review: {result.Error}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[POIDetailPage] ✗ Review sync error: {ex.Message}");
                }
            });
            
            await DisplayAlert("Cảm ơn!", "Đánh giá của bạn đã được ghi nhận!", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[POIDetailPage] Error submitting review: {ex.Message}");
            await DisplayAlert("Lỗi", "Không thể gửi đánh giá. Vui lòng thử lại!", "OK");
        }
    }

    private void OnStar1Tapped(object? sender, TappedEventArgs e) { _selectedRating = 1; UpdateStarDisplay(); }
    private void OnStar2Tapped(object? sender, TappedEventArgs e) { _selectedRating = 2; UpdateStarDisplay(); }
    private void OnStar3Tapped(object? sender, TappedEventArgs e) { _selectedRating = 3; UpdateStarDisplay(); }
    private void OnStar4Tapped(object? sender, TappedEventArgs e) { _selectedRating = 4; UpdateStarDisplay(); }
    private void OnStar5Tapped(object? sender, TappedEventArgs e) { _selectedRating = 5; UpdateStarDisplay(); }

    private void UpdateStarDisplay()
    {
        var stars = new[] { star1, star2, star3, star4, star5 };
        for (int i = 0; i < 5; i++)
        {
            if (stars[i] != null)
            {
                stars[i].Text = i < _selectedRating ? "★" : "☆"; // Filled vs empty star
            }
        }
    }
}

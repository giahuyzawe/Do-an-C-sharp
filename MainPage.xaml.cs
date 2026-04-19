using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using FoodStreetGuide.Services;
using FoodStreetGuide.Models;
using FoodStreetGuide.Platforms.Android;
using FoodStreetGuide.Database;
using System.Linq;

namespace FoodStreetGuide;

public partial class MainPage : ContentPage
{
    private readonly ILocationService? _locationService;
    private readonly IGeofenceEngine? _geofenceEngine;
    private readonly DatabaseService _databaseService;
    private readonly ITTSService? _ttsService;
    private readonly ISettingsService? _settingsService;
    private readonly INarrationEngine? _narrationEngine;
    private readonly ILocalizationService? _localizationService;
    private readonly ISavedPOIService? _savedPOIService;
    private Pin? _currentLocationPin;
    private readonly List<Pin> _poiPins = new();
    private readonly Dictionary<int, Pin> _poiPinDictionary = new();
    private bool _geofenceEnabled;
    private bool _locationSubscribed;
    private POI? _currentNearestPOI;
    
    // Expandable Bottom Sheet State
    private bool _isBottomSheetExpanded = false;
    private const double COLLAPSED_HEIGHT = 280;
    private const double EXPANDED_HEIGHT = 650;
    
    // Review State
    private int _selectedRating = 0;
    private bool _isReviewFormVisible = false;

    public MainPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        _locationService = ServiceProviderHelper.GetService<ILocationService>();
        _geofenceEngine = ServiceProviderHelper.GetService<IGeofenceEngine>();
        _ttsService = ServiceProviderHelper.GetService<ITTSService>();
        _settingsService = ServiceProviderHelper.GetService<ISettingsService>();
        _localizationService = ServiceProviderHelper.GetService<ILocalizationService>();
        _savedPOIService = ServiceProviderHelper.GetService<ISavedPOIService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            // Always subscribe to events when page appears
            if (_geofenceEngine != null)
            {
                _geofenceEngine.POIEntered += OnPOIEntered;
                _geofenceEngine.POIExited += OnPOIExited;
                _geofenceEngine.NearestPOIChanged += OnNearestPOIChanged;
                
                // Enable geofence if not already enabled
                if (!_geofenceEngine.IsEnabled)
                {
                    var pois = await _databaseService.GetPOIsAsync();
                    System.Diagnostics.Debug.WriteLine($"[OnAppearing] Loaded {pois.Count} POIs from database");
                    
                    // Always enable geofence, even with empty POIs
                    // POIs will be updated when API sync completes
                    if (pois.Count > 0)
                    {
                        _geofenceEngine.SetPOIs(pois);
                        DisplayPOIMarkers(pois);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[OnAppearing] Database empty, geofence enabled but no POIs yet");
                    }
                    
                    _geofenceEngine.Enable();
                    _geofenceEnabled = true;
                }
                else
                {
                    // Just refresh markers
                    var pois = await _databaseService.GetPOIsAsync();
                    System.Diagnostics.Debug.WriteLine($"[OnAppearing] Refreshed {pois.Count} POIs from database");
                    DisplayPOIMarkers(pois, _currentNearestPOI?.Id);
                }
                
                // Auto sync disabled - ApiService in App.xaml.cs handles POI loading
                // _ = AutoSyncFromWebAsync();
            }
            
            // Handle deep link from QR scan - navigate to POI detail
            _ = HandleDeepLinkAsync();
            
            if (map != null && map.VisibleRegion == null)
            {
                var position = new Location(10.762622, 106.660172);
                map.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        position,
                        Distance.FromKilometers(1)
                    )
                );
            }
            
            // Auto-enable passive tracking (no button needed)
            if (_locationService != null && !_locationSubscribed)
            {
                SubscribeToLocationUpdates();
                if (!_locationService.IsTracking)
                {
                    _ = _locationService.StartTrackingAsync();
                    System.Diagnostics.Debug.WriteLine("[OnAppearing] Auto-started location tracking (passive)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[OnAppearing] Location tracking already active");
                }
            }
            
            // Request and display current location
            _ = UpdateCurrentLocationAsync();
            
            UpdateTrackingButtonState();
            
            // Update UI language
            UpdateLanguage();
            
            // Subscribe to language changes
            if (_localizationService != null)
                _localizationService.LanguageChanged += OnLanguageChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnAppearing] Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Handle deep link from QR scan - check QR token and navigate to POI detail
    /// </summary>
    private async Task HandleDeepLinkAsync()
    {
        try
        {
            // Check if we have a deep link token from MainActivity
            var token = MainActivity.DeepLinkToken;
            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("[HandleDeepLink] No deep link token");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[HandleDeepLink] Processing QR token: {token}");
            
            // Clear token so we don't process again
            MainActivity.DeepLinkToken = null;
            
            // Wait a bit for POIs to load
            await Task.Delay(1000);
            
            // Call API to verify QR and get POI info
            var apiService = new ApiService();
            var deviceId = Preferences.Get("DeviceId", "");
            var checkResult = await apiService.CheckQRAsync(token, deviceId);
            
            if (!checkResult.Success || checkResult.Data == null)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleDeepLink] QR check failed: {checkResult.Error}");
                await DisplayAlert("Lỗi", checkResult.Error ?? "QR không hợp lệ", "OK");
                return;
            }
            
            // Get POI ID from response
            var poiId = checkResult.Data.PoiId;
            System.Diagnostics.Debug.WriteLine($"[HandleDeepLink] QR valid, POI ID: {poiId}");
            
            // Find POI in database
            var poi = await _databaseService.GetPOIAsync(poiId);
            if (poi == null)
            {
                // Try to fetch from API
                var poisResponse = await apiService.GetPOIsAsync();
                var pois = poisResponse.Data?.Data ?? Array.Empty<POIApiData>();
                var poiData = pois.FirstOrDefault(p => p.Id == poiId);
                
                if (poiData == null)
                {
                    await DisplayAlert("Lỗi", "Không tìm thấy nhà hàng", "OK");
                    return;
                }
                
                // Convert to POI model
                poi = new POI
                {
                    Id = poiData.Id,
                    NameVi = poiData.NameVi ?? "",
                    NameEn = poiData.NameEn ?? "",
                    Address = poiData.Address ?? "",
                    DescriptionVi = poiData.Description ?? "",
                    Latitude = poiData.Latitude ?? 0,
                    Longitude = poiData.Longitude ?? 0,
                    ImageUrl = poiData.ImageUrl ?? "",
                    Phone = poiData.Phone ?? "",
                    OpeningHours = poiData.OpeningHours ?? "",
                    Rating = poiData.Rating,
                    Status = poiData.Status ?? "active"
                };
                
                // Save to database
                await _databaseService.SavePOIAsync(poi);
            }
            
            // Show check-in success - include qrToken for tracking
            await apiService.PostAnalyticsAsync("check_in", deviceId, poiId, token);
            
            // Navigate to POI detail
            System.Diagnostics.Debug.WriteLine($"[HandleDeepLink] Navigating to POI: {poi.NameVi}");
            var detailPage = new POIDetailPage(poi);
            await Navigation.PushAsync(detailPage);
            
            // Show success message
            await DisplayAlert("🎉 Check-in thành công!", $"Bạn đã check-in tại {poi.NameVi}", "Xem chi tiết");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HandleDeepLink] Error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // NOTE: Don't unsubscribe from location updates when just switching tabs
        // The background service continues running, and we'll resubscribe in OnAppearing
        // UnsubscribeFromLocationUpdates(); // REMOVED - causes tracking to appear stopped when returning
        
        // Unsubscribe events but keep geofence running in background
        if (_geofenceEngine != null)
        {
            _geofenceEngine.POIEntered -= OnPOIEntered;
            _geofenceEngine.POIExited -= OnPOIExited;
            _geofenceEngine.NearestPOIChanged -= OnNearestPOIChanged;
            // Do NOT Disable() - let it run in background for tracking
            // Geofence engine continues running even when app is in background
        }
        
        // Unsubscribe from language changes
        if (_localizationService != null)
            _localizationService.LanguageChanged -= OnLanguageChanged;
    }
    
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLanguage();
    }

    private void UpdateTrackingButtonState()
    {
        // Tracking UI removed - passive tracking only
        // Method kept for compatibility but does nothing
    }

    private void SubscribeToLocationUpdates()
    {
        if (_locationService != null && !_locationSubscribed)
        {
            _locationService.LocationUpdated += OnLocationUpdated;
            _locationSubscribed = true;
            UpdateTrackingButtonState();
        }
    }

    private void UnsubscribeFromLocationUpdates()
    {
        if (_locationService != null && _locationSubscribed)
        {
            _locationService.LocationUpdated -= OnLocationUpdated;
            _locationSubscribed = false;
        }
    }

    private void OnLocationUpdated(object? sender, LocationUpdatedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[OnLocationUpdated] ====== TRACKING UPDATE ======");
        System.Diagnostics.Debug.WriteLine($"[OnLocationUpdated] Location: {e.Latitude:F6}, {e.Longitude:F6}");
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var currentLocation = new Location(e.Latitude, e.Longitude);
            
            if (_geofenceEngine != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OnLocationUpdated] Geofence enabled: {_geofenceEngine.IsEnabled}, POI count: {_geofenceEngine.POICount}");
                _geofenceEngine.UpdateLocation(e.Latitude, e.Longitude);
                System.Diagnostics.Debug.WriteLine($"[OnLocationUpdated] Geofence engine updated");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OnLocationUpdated] ❌ Geofence engine is NULL!");
            }
            
            if (_currentLocationPin == null)
            {
                _currentLocationPin = new Pin
                {
                    Label = "📍 You",
                    Location = currentLocation,
                    Type = PinType.Generic
                };
                map.Pins.Add(_currentLocationPin);
            }
            else
            {
                _currentLocationPin.Location = currentLocation;
            }

            map.MoveToRegion(MapSpan.FromCenterAndRadius(currentLocation, Distance.FromKilometers(0.5)));
        });
    }

    private async void OnTrackButtonClicked(object sender, EventArgs e)
    {
        if (_locationService == null)
        {
            await DisplayAlert("Error", "Location service not available", "OK");
            return;
        }

        try
        {
            if (_locationSubscribed)
            {
                // Stop tracking
                UnsubscribeFromLocationUpdates();
                if (_locationService.IsTracking)
                    await _locationService.StopTrackingAsync();
            }
            else
            {
                // Start tracking
                System.Diagnostics.Debug.WriteLine($"[Track] Starting tracking... Geofence enabled: {_geofenceEngine?.IsEnabled}, POIs: {_geofenceEngine?.POICount}");
                
                // Ensure geofence is enabled with POIs
                if (_geofenceEngine != null && (!_geofenceEngine.IsEnabled || _geofenceEngine.POICount == 0))
                {
                    System.Diagnostics.Debug.WriteLine("[Track] Re-initializing geofence engine...");
                    var pois = await _databaseService.GetPOIsAsync();
                    _geofenceEngine.SetPOIs(pois);
                    _geofenceEngine.Enable();
                    System.Diagnostics.Debug.WriteLine($"[Track] Geofence re-initialized with {pois.Count} POIs");
                }
                
                SubscribeToLocationUpdates();
                if (!_locationService.IsTracking)
                    await _locationService.StartTrackingAsync();
                
                // Wait for location updates to come in
                // Geofence engine will automatically trigger POI if:
                // 1. User is inside POI radius
                // 2. POI is not in cooldown
                // DO NOT manually show POI card here - let geofence handle it
                System.Diagnostics.Debug.WriteLine("[Track] Waiting for geofence triggers...");
            }
            UpdateTrackingButtonState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Track] Error: {ex.Message}");
            await DisplayAlert("Error", $"Cannot change tracking: {ex.Message}", "OK");
        }
    }

    private async Task FindAndShowNearestPOIAsync()
    {
        try
        {
            if (_currentLocationPin?.Location == null)
            {
                System.Diagnostics.Debug.WriteLine("[FindAndShowNearestPOIAsync] Current location not available");
                return;
            }

            var pois = await _databaseService.GetPOIsAsync();
            if (pois.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[FindAndShowNearestPOIAsync] No POIs in database");
                return;
            }

            // Find nearest POI
            var userLocation = _currentLocationPin.Location;
            POI? nearestPOI = null;
            double minDistance = double.MaxValue;

            foreach (var poi in pois)
            {
                var poiLocation = new Location(poi.Latitude, poi.Longitude);
                var distance = userLocation.CalculateDistance(poiLocation, DistanceUnits.Kilometers) * 1000; // meters

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPOI = poi;
                }
            }

            if (nearestPOI != null)
            {
                System.Diagnostics.Debug.WriteLine($"[FindAndShowNearestPOIAsync] Found nearest POI: {nearestPOI.NameVi} ({minDistance:F0}m)");
                
                _currentNearestPOI = nearestPOI;
                
                // Show POI card
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    poiCard.IsVisible = true;
                    // Control bar removed
                    var lang = _settingsService?.GetLanguage() ?? "vi";
                    poiNameLabel.Text = lang == "en" ? nearestPOI.NameEn : nearestPOI.NameVi;
                    poiDistanceLabel.Text = $"{minDistance:F0}m";
                    poiAddressLabel.Text = nearestPOI.Address;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FindAndShowNearestPOIAsync] Error: {ex.Message}");
        }
    }

    private async void OnFindNearestPOI(object sender, EventArgs e)
    {
        try
        {
            var pois = await _databaseService.GetPOIsAsync();
            
            if (pois.Count == 0)
            {
                await DisplayAlert("Notice", "No POI in database", "OK");
                return;
            }

            var currentLocation = _currentLocationPin?.Location;
            if (currentLocation == null)
            {
                await DisplayAlert("Notice", "Please enable tracking first", "OK");
                return;
            }

            POI? nearest = null;
            double minDistance = double.MaxValue;

            foreach (var poi in pois)
            {
                var poiLocation = new Location(poi.Latitude, poi.Longitude);
                var distance = currentLocation.CalculateDistance(poiLocation, DistanceUnits.Kilometers);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = poi;
                }
            }

            if (nearest != null)
            {
                var distanceMeters = minDistance * 1000;
                await DisplayAlert(
                    "Nearest POI",
                    $"{nearest.NameVi}\nDistance: {distanceMeters:F0}m\nRadius: {nearest.Radius}m",
                    "OK");

                map.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        new Location(nearest.Latitude, nearest.Longitude),
                        Distance.FromMeters(100)
                    )
                );
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnPOIEntered(object? sender, GeofenceEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[OnPOIEntered] START - POI: {e.POI.NameVi}, Distance: {e.DistanceMeters:F1}m");
        
        // Update POI card UI immediately when triggered
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            System.Diagnostics.Debug.WriteLine($"[OnPOIEntered] Updating POI card for triggered POI: {e.POI.NameVi}");
            
            _currentNearestPOI = e.POI;
            
            poiCard.IsVisible = true;
            CollapseBottomSheet();
            
            var lang2 = _settingsService?.GetLanguage() ?? "vi";
            poiNameLabel.Text = lang2 == "en" ? e.POI.NameEn : e.POI.NameVi;
            poiDistanceLabel.Text = $"{e.DistanceMeters:F0}m";
            poiAddressLabel.Text = e.POI.Address ?? "Không có địa chỉ";
            
            // Populate expanded details
            if (poiHoursLabel != null)
                poiHoursLabel.Text = e.POI.OpeningHours ?? "Chưa có thông tin";
            if (poiPhoneLabel != null)
                poiPhoneLabel.Text = e.POI.Phone ?? "Chưa có thông tin";
            if (poiDescriptionLabel != null)
                poiDescriptionLabel.Text = lang2 == "en" ? (e.POI.DescriptionEn ?? "Không có mô tả") : (e.POI.DescriptionVi ?? "Không có mô tả");
            
            // Update status - check if open or closed
            UpdatePOIStatus(e.POI, lang2);
            
            // Load images into carousel
            if (poiImageCarousel != null)
            {
                var images = GetPOIImages(e.POI);
                poiImageCarousel.ItemsSource = images;
            }
            
            // Update save button state
            if (saveHeartLabel != null && _savedPOIService != null)
            {
                bool isSaved = _savedPOIService.IsSaved(e.POI);
                saveHeartLabel.TextColor = isSaved ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            }
        });
        
        // Then do TTS narration
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                var language = _settingsService?.GetLanguage() ?? "vi";
                string message;
                string ttsVoice;
                
                if (language == "en")
                {
                    message = $"You are approaching {e.POI.NameEn}. {e.POI.DescriptionEn}";
                    ttsVoice = "en-US";
                }
                else
                {
                    message = $"Bạn đang đến gần {e.POI.NameVi}. {e.POI.DescriptionVi}";
                    ttsVoice = "vi-VN";
                }
                
                System.Diagnostics.Debug.WriteLine($"[TTS] Preparing ({language}): '{message}' for POI {e.POI.Id}:{e.POI.NameVi}");
                
                if (_ttsService != null && _settingsService?.GetNarrationEnabled() != false)
                {
                    await _ttsService.StopAsync();
                    await _ttsService.SpeakAsync(message, ttsVoice);
                    System.Diagnostics.Debug.WriteLine($"[TTS] Completed for {e.POI.NameVi}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[TTS] Skipped (narration disabled or service unavailable)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Exception: {ex.Message}");
            }
        });
    }

    private void OnPOIExited(object? sender, GeofenceEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[OnPOIExited] EXITED POI: {e.POI.NameVi}, Distance: {e.DistanceMeters:F1}m");
        
        // Hide POI card when user exits the geofence
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Only hide if this is the currently displayed POI
            if (_currentNearestPOI?.Id == e.POI.Id)
            {
                System.Diagnostics.Debug.WriteLine($"[OnPOIExited] Hiding POI card for {e.POI.NameVi}");
                poiCard.IsVisible = false;
                _currentNearestPOI = null;
                
                // Control bar removed
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OnPOIExited] Not hiding - current POI is different");
            }
        });
    }

    private void OnNearestPOIChanged(object? sender, NearestPOIChangedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[OnNearestPOIChanged] START - NewPOI: {e.NewPOI?.NameVi}, Distance: {e.DistanceMeters:F0}m");
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.NewPOI != null)
            {
                // Only update the reference, DON'T show POI card here
                // POI card will only show when OnPOIEntered is triggered (not on cooldown)
                _currentNearestPOI = e.NewPOI;
                System.Diagnostics.Debug.WriteLine($"[OnNearestPOIChanged] Updated reference to: {e.NewPOI.NameVi} (card NOT shown - waiting for trigger)");

                // Update map markers to highlight nearest POI
                _ = _databaseService.GetPOIsAsync().ContinueWith(task =>
                {
                    if (task.Result != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            DisplayPOIMarkers(task.Result, e.NewPOI.Id);
                        });
                    }
                });
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OnNearestPOIChanged] NewPOI is null, clearing reference");
                _currentNearestPOI = null;
            }
        });
    }

    private void DisplayPOIMarkers(List<POI> pois, int? nearestPOIId = null)
    {
        if (map == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Clear existing pins
            foreach (var pin in _poiPins)
            {
                map.Pins.Remove(pin);
            }
            _poiPins.Clear();
            _poiPinDictionary.Clear();

            // Add all pins with highlight for nearest
            foreach (var poi in pois)
            {
                bool isNearest = nearestPOIId.HasValue && poi.Id == nearestPOIId.Value;
                var poiPin = new Pin
                {
                    Label = isNearest ? "★ " + poi.NameVi : poi.NameVi,
                    Location = new Location(poi.Latitude, poi.Longitude),
                    Type = isNearest ? PinType.SavedPin : PinType.Place,
                    Address = poi.Address
                };
                
                // Add click handler to show POI card
                poiPin.MarkerClicked += (s, e) =>
                {
                    e.HideInfoWindow = true;
                    ShowPOICardForMarker(poi);
                };
                
                map.Pins.Add(poiPin);
                _poiPins.Add(poiPin);
                _poiPinDictionary[poi.Id] = poiPin;
            }
            
            System.Diagnostics.Debug.WriteLine($"[DisplayPOIMarkers] Added {pois.Count} pins, nearest: {nearestPOIId}");
        });
    }

    private void ShowPOICardForMarker(POI poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] ===== START =====");
            System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] POI: {poi.NameVi}, ID: {poi.Id}");
            System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] OpeningHours: {poi.OpeningHours}");
            System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] Phone: {poi.Phone}");
            System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] DescriptionVi: {poi.DescriptionVi}");
            
            _currentNearestPOI = poi;
            
            // Show POI card with details
            poiCard.IsVisible = true;
            CollapseBottomSheet(); // Start collapsed
            
            // Basic info
            var lang = _settingsService?.GetLanguage() ?? "vi";
            poiNameLabel.Text = lang == "en" ? poi.NameEn : poi.NameVi;
            poiAddressLabel.Text = poi.Address ?? "Không có địa chỉ";
            
            // Expanded details
            if (poiHoursLabel != null)
            {
                poiHoursLabel.Text = poi.OpeningHours ?? "Chưa có thông tin";
                System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] Set Hours Label: {poiHoursLabel.Text}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] ERROR: poiHoursLabel is NULL!");
            }
            
            if (poiPhoneLabel != null)
            {
                poiPhoneLabel.Text = poi.Phone ?? "Chưa có thông tin";
                System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] Set Phone Label: {poiPhoneLabel.Text}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] ERROR: poiPhoneLabel is NULL!");
            }
            
            if (poiDescriptionLabel != null)
            {
                poiDescriptionLabel.Text = lang == "en" ? (poi.DescriptionEn ?? "Không có mô tả") : (poi.DescriptionVi ?? "Không có mô tả");
                System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] Set Description Label: {poiDescriptionLabel.Text}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] ERROR: poiDescriptionLabel is NULL!");
            }
            
            // Calculate distance
            _ = UpdateDistanceLabelAsync(poi);
            
            // Load images if available
            if (!string.IsNullOrEmpty(poi.ImageUrl))
            {
                var images = poi.ImageUrl.Split(',').Select(i => i.Trim()).Where(i => !string.IsNullOrEmpty(i)).ToList();
                poiImageCarousel.ItemsSource = images;
                poiImageIndicator.IsVisible = images.Count > 1;
            }
            else
            {
                poiImageCarousel.ItemsSource = new List<string> { "placeholder_restaurant.png" };
                poiImageIndicator.IsVisible = false;
            }
            
            System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] ===== END =====");
        });
    }

    private async Task UpdateDistanceLabelAsync(POI poi)
    {
        try
        {
            if (_locationService != null)
            {
                var currentLocation = await _locationService.GetCurrentLocationAsync();
                if (currentLocation != null)
                {
                    var distanceMeters = CalculateDistanceMeters(
                        currentLocation.Latitude, currentLocation.Longitude,
                        poi.Latitude, poi.Longitude);
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (distanceMeters < 1000)
                            poiDistanceLabel.Text = $"{distanceMeters:F0}m";
                        else
                            poiDistanceLabel.Text = $"{distanceMeters / 1000:F1}km";
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateDistanceLabelAsync] Error: {ex.Message}");
        }
    }

    private async void OnDirectionsClicked(object? sender, EventArgs e)
    {
        if (_currentNearestPOI == null) return;
        
        try
        {
            var lang = _settingsService?.GetLanguage() ?? "vi";
            var name = lang == "en" ? _currentNearestPOI.NameEn : _currentNearestPOI.NameVi;
            
            // Use Google Maps URL for navigation
            var googleMapsUrl = $"https://www.google.com/maps/dir/?api=1&destination={_currentNearestPOI.Latitude},{_currentNearestPOI.Longitude}&destination_place_id={Uri.EscapeDataString(name)}";
            
            await Launcher.OpenAsync(new Uri(googleMapsUrl));
        }
        catch (Exception ex)
        {
            var loc = _localizationService;
            await DisplayAlert(loc?.GetString("Alert_Error") ?? "Lỗi", $"Không thể mở bản đồ: {ex.Message}", "OK");
        }
    }

    private void OnRecenterTapped(object sender, EventArgs e)
    {
        Location? targetLocation = _currentLocationPin?.Location;
        
        // If no tracking yet, use default location (Ho Chi Minh City)
        if (targetLocation == null)
        {
            targetLocation = new Location(10.762622, 106.660172);
        }
        
        map.MoveToRegion(MapSpan.FromCenterAndRadius(targetLocation, Distance.FromKilometers(0.5)));
    }

    private void OnZoomInClicked(object sender, EventArgs e)
    {
        if (map?.VisibleRegion != null)
        {
            var newRadius = map.VisibleRegion.Radius.Kilometers / 2;
            map.MoveToRegion(MapSpan.FromCenterAndRadius(map.VisibleRegion.Center, Distance.FromKilometers(newRadius)));
        }
    }

    private void OnZoomOutClicked(object sender, EventArgs e)
    {
        if (map?.VisibleRegion != null)
        {
            var newRadius = map.VisibleRegion.Radius.Kilometers * 2;
            map.MoveToRegion(MapSpan.FromCenterAndRadius(map.VisibleRegion.Center, Distance.FromKilometers(newRadius)));
        }
    }

    private void OnClosePOICardClicked(object? sender, EventArgs e)
    {
        poiCard.IsVisible = false;
        // controlBar removed
    }

    private void OnClosePOICardSwiped(object? sender, SwipedEventArgs e)
    {
        poiCard.IsVisible = false;
        // controlBar removed
    }

    private async void OnNarrateClicked(object? sender, EventArgs e)
    {
        if (_currentNearestPOI == null || _ttsService == null) return;
        
        var language = _settingsService?.GetLanguage() ?? "vi";
        string message;
        string ttsVoice;
        
        if (language == "en")
        {
            message = $"You are approaching {_currentNearestPOI.NameEn}. {_currentNearestPOI.DescriptionEn}";
            ttsVoice = "en-US";
        }
        else
        {
            message = $"Bạn đang đến gần {_currentNearestPOI.NameVi}. {_currentNearestPOI.DescriptionVi}";
            ttsVoice = "vi-VN";
        }
        
        await _ttsService.SpeakAsync(message, ttsVoice);
    }

    private void OnDetailsClicked(object? sender, EventArgs e)
    {
        // Simply expand the bottom sheet to show full details
        ExpandBottomSheet();
    }

    private async void OnCallClicked(object? sender, EventArgs e)
    {
        if (_currentNearestPOI == null) return;
        
        var phone = _currentNearestPOI.Phone;
        if (string.IsNullOrEmpty(phone))
        {
            await DisplayAlert("Thông báo", "Nhà hàng chưa cập nhật số điện thoại", "OK");
            return;
        }
        
        try
        {
            var phoneUri = new Uri($"tel:{phone}");
            await Launcher.OpenAsync(phoneUri);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể gọi điện: {ex.Message}", "OK");
        }
    }

    private async void OnFindNearestClicked(object? sender, EventArgs e)
    {
        if (_currentNearestPOI == null)
        {
            await DisplayAlert("Info", "No nearby POI found. Start tracking to find nearby restaurants.", "OK");
            return;
        }

        // Center map on nearest POI
        var poiLocation = new Location(_currentNearestPOI.Latitude, _currentNearestPOI.Longitude);
        map.MoveToRegion(
            MapSpan.FromCenterAndRadius(
                poiLocation,
                Distance.FromMeters(100)
            )
        );

        // Show POI card
        MainThread.BeginInvokeOnMainThread(() =>
        {
            poiCard.IsVisible = true;
            poiNameLabel.Text = _currentNearestPOI.NameVi;
            poiDistanceLabel.Text = "Nearest restaurant";
            poiAddressLabel.Text = _currentNearestPOI.Address ?? "Address not available";
            
        });
    }

    private void OnNarrateManualClicked(object? sender, EventArgs e)
    {
        if (_currentNearestPOI == null || _ttsService == null) return;
        
        var message = $"You are approaching {_currentNearestPOI.NameVi}. {_currentNearestPOI.DescriptionVi}";
        _ttsService.SpeakAsync(message);
    }

    // NEW EVENT HANDLERS
    private void OnFilterTapped(object? sender, TappedEventArgs e)
    {
        // Open filter dialog
        DisplayAlert("Filter", "Filter options will be implemented here", "OK");
    }

    private void OnSearchTapped(object? sender, TappedEventArgs e)
    {
        // Open search page
        DisplayAlert("Search", "Search page will be implemented here", "OK");
    }

    private void OnVoiceToggleClicked(object? sender, EventArgs e)
    {
        if (_currentNearestPOI == null || _ttsService == null)
        {
            DisplayAlert("Info", "No nearby POI to narrate", "OK");
            return;
        }

        var message = $"You are approaching {_currentNearestPOI.NameVi}. {_currentNearestPOI.DescriptionVi}";
        _ttsService.SpeakAsync(message);
    }

    private void OnSaveClicked(object? sender, TappedEventArgs e)
    {
        if (_currentNearestPOI == null)
        {
            var loc = _localizationService;
            DisplayAlert(loc?.GetString("Alert_NoPOI") ?? "Thông báo", loc?.GetString("Alert_NoPOI") ?? "Không có địa điểm để lưu", "OK");
            return;
        }
        
        if (_savedPOIService == null) return;
        
        // Toggle save state using service
        bool isNowSaved = !_savedPOIService.IsSaved(_currentNearestPOI);
        _savedPOIService.ToggleSave(_currentNearestPOI);
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (saveHeartLabel != null)
            {
                saveHeartLabel.TextColor = isNowSaved ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            }
        });
        
        var loc2 = _localizationService;
        var name = _settingsService?.GetLanguage() == "en" ? _currentNearestPOI.NameEn : _currentNearestPOI.NameVi;
        var message = isNowSaved 
            ? string.Format(loc2?.GetString("Alert_Saved") ?? "Đã lưu {0}", name)
            : string.Format(loc2?.GetString("Alert_Unsaved") ?? "Đã bỏ lưu {0}", name);
        DisplayAlert(loc2?.GetString("Alert_NoPOI") ?? "Thông báo", message, "OK");
        
        System.Diagnostics.Debug.WriteLine($"[OnSaveClicked] POI '{name}' is now {(isNowSaved ? "saved" : "unsaved")}");
    }

    // QR Scan button - Navigate to QR Scan page
    private async void OnQRScanClicked(object? sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new QRScanPage());
        }
        catch (Exception ex)
        {
            var loc = _localizationService;
            await DisplayAlert(loc?.GetString("Alert_Error") ?? "Lỗi", $"Không thể mở QR scanner: {ex.Message}", "OK");
        }
    }

    // REMOVED: Audio button moved to POI card only
    // Auto narration is controlled via Settings tab

    // Language Update Method
    private async Task UpdateCurrentLocationAsync()
    {
        try
        {
            // Try to get current location
            var location = await Geolocation.GetLastKnownLocationAsync() 
                ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
            
            if (location != null && map != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Update or create location pin
                    if (_currentLocationPin == null)
                    {
                        _currentLocationPin = new Pin
                        {
                            Label = "📍 You",
                            Location = location,
                            Type = PinType.Generic
                        };
                        map.Pins.Add(_currentLocationPin);
                    }
                    else
                    {
                        _currentLocationPin.Location = location;
                    }
                    
                    // Move map to current location
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.5)));
                    System.Diagnostics.Debug.WriteLine($"[UpdateCurrentLocationAsync] Map centered at: {location.Latitude:F6}, {location.Longitude:F6}");
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateCurrentLocationAsync] Error: {ex.Message}");
        }
    }

    private void UpdateLanguage()
    {
        if (_localizationService == null) return;
        var loc = _localizationService;
        
        // Control bar buttons removed
        // if (trackButton != null)
        //     trackButton.Text = _locationSubscribed ? loc.GetString("Stop") : loc.GetString("Tracking");
        // if (nearestButton != null)
        //     nearestButton.Text = loc.GetString("Nearest");
        // if (qrButton != null)
        //     qrButton.Text = loc.GetString("QR");
        
        // POI card labels
        if (listenLabel != null)
            listenLabel.Text = loc.GetString("POI_Listen");
        if (directionsLabel != null)
            directionsLabel.Text = loc.GetString("POI_Directions");
        
        // Update search entry placeholder
        if (searchEntry != null)
            searchEntry.Placeholder = loc.GetString("SearchPlaceholder");
    }

    // SEARCH FUNCTIONALITY
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (clearSearchLabel != null)
        {
            clearSearchLabel.IsVisible = !string.IsNullOrWhiteSpace(e.NewTextValue);
        }
    }

    private async void OnSearchCompleted(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(searchEntry?.Text))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập tên quán hoặc món ăn", "OK");
            return;
        }
        
        var keyword = searchEntry.Text.Trim().ToLower();
        await SearchNearbyPOIs(keyword);
    }

    private void OnClearSearchTapped(object? sender, TappedEventArgs e)
    {
        if (searchEntry != null)
        {
            searchEntry.Text = "";
            clearSearchLabel.IsVisible = false;
        }
        
        // Hide search results list
        if (searchResultsScroll != null)
            searchResultsScroll.IsVisible = false;
        if (searchResultsList != null)
            searchResultsList.Children.Clear();
        _currentSearchResults.Clear();
        
        // Show all POIs again
        _ = LoadAllPOIsAsync();
    }

    // Store search results for chip selection
    private List<POI> _currentSearchResults = new();

    private async Task SearchNearbyPOIs(string keyword)
    {
        try
        {
            // Show loading indicator
            var pois = await _databaseService.GetPOIsAsync();
            if (pois == null || pois.Count == 0)
            {
                await DisplayAlert("Thông báo", "Không tìm thấy nhà hàng nào", "OK");
                return;
            }
            
            // Filter POIs by name or description
            var language = _settingsService?.GetLanguage() ?? "vi";
            var matchedPOIs = pois.Where(p => 
                (language == "en" ? p.NameEn : p.NameVi).ToLower().Contains(keyword) ||
                (language == "en" ? p.DescriptionEn : p.DescriptionVi).ToLower().Contains(keyword)
            ).ToList();
            
            if (matchedPOIs.Count == 0)
            {
                await DisplayAlert("Thông báo", $"Không tìm thấy quán nào phù hợp với \"{keyword}\"", "OK");
                return;
            }
            
            // Store results
            _currentSearchResults = matchedPOIs;
            
            // Clear existing pins and show matched POIs
            MainThread.BeginInvokeOnMainThread(() =>
            {
                map.Pins.Clear();
                _poiPins.Clear();
                _poiPinDictionary.Clear();
                
                foreach (var poi in matchedPOIs)
                {
                    var pin = new Pin
                    {
                        Location = new Location(poi.Latitude, poi.Longitude),
                        Label = language == "en" ? poi.NameEn : poi.NameVi,
                        Address = poi.Address,
                        Type = PinType.Place
                    };
                    map.Pins.Add(pin);
                    _poiPins.Add(pin);
                    _poiPinDictionary[poi.Id] = pin;
                }
                
                // Show search results list
                ShowSearchResultsList(matchedPOIs);
                
                // Center map on first result and show POI card
                if (matchedPOIs.Count > 0)
                {
                    SelectSearchResult(0);
                }
                
                // Show search result count
                clearSearchLabel.IsVisible = true;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SearchNearbyPOIs] Error: {ex.Message}");
            await DisplayAlert("Lỗi", "Không thể tìm kiếm, vui lòng thử lại", "OK");
        }
    }

    private void ShowSearchResultsList(List<POI> pois)
    {
        if (searchResultsList == null || searchResultsScroll == null) return;
        
        searchResultsList.Children.Clear();
        
        for (int i = 0; i < pois.Count; i++)
        {
            var poi = pois[i];
            var index = i; // Capture for closure
            
            var chip = new Frame
            {
                BackgroundColor = i == 0 ? Color.FromArgb("#FF6B35") : Color.FromArgb("#F0F0F0"),
                CornerRadius = 16,
                Padding = new Thickness(12, 6),
                BorderColor = Colors.Transparent,
                HasShadow = false
            };
            
            var label = new Label
            {
                Text = poi.NameVi,
                FontSize = 13,
                TextColor = i == 0 ? Colors.White : Color.FromArgb("#333333"),
                FontAttributes = i == 0 ? FontAttributes.Bold : FontAttributes.None
            };
            
            chip.Content = label;
            
            // Add tap gesture
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnSearchResultChipTapped(index);
            chip.GestureRecognizers.Add(tapGesture);
            
            searchResultsList.Children.Add(chip);
        }
        
        searchResultsScroll.IsVisible = true;
    }

    private void OnSearchResultChipTapped(int index)
    {
        System.Diagnostics.Debug.WriteLine($"[SearchResultChip] Tapped index {index}");
        SelectSearchResult(index);
        
        // Update chip colors
        for (int i = 0; i < searchResultsList.Children.Count; i++)
        {
            if (searchResultsList.Children[i] is Frame chip && chip.Content is Label label)
            {
                chip.BackgroundColor = i == index ? Color.FromArgb("#FF6B35") : Color.FromArgb("#F0F0F0");
                label.TextColor = i == index ? Colors.White : Color.FromArgb("#333333");
                label.FontAttributes = i == index ? FontAttributes.Bold : FontAttributes.None;
            }
        }
    }

    private void SelectSearchResult(int index)
    {
        if (index < 0 || index >= _currentSearchResults.Count) return;
        
        var poi = _currentSearchResults[index];
        _currentNearestPOI = poi;
        
        var language = _settingsService?.GetLanguage() ?? "vi";
        
        // Center map
        map.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(poi.Latitude, poi.Longitude),
            Distance.FromKilometers(0.5)
        ));
        
        // Update POI card
        if (poiCard != null)
        {
            poiCard.IsVisible = true;
            // controlBar removed
            
            if (poiNameLabel != null)
                poiNameLabel.Text = language == "en" ? poi.NameEn : poi.NameVi;
            if (poiAddressLabel != null)
                poiAddressLabel.Text = poi.Address;
            if (poiDistanceLabel != null)
                poiDistanceLabel.Text = $"{index + 1}/{_currentSearchResults.Count}";
            if (poiRatingLabel != null)
                poiRatingLabel.Text = "⭐ 4.5";
            
            // Load images
            if (poiImageCarousel != null)
            {
                var images = GetPOIImages(poi);
                poiImageCarousel.ItemsSource = images;
            }
            
            // Update save button
            if (saveHeartLabel != null && _savedPOIService != null)
            {
                bool isSaved = _savedPOIService.IsSaved(poi);
                saveHeartLabel.TextColor = isSaved ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[SelectSearchResult] Selected: {poi.NameVi} ({index + 1}/{_currentSearchResults.Count})");
    }

    private async Task LoadAllPOIsAsync()
    {
        try
        {
            var pois = await _databaseService.GetPOIsAsync();
            if (pois != null)
            {
                var language = _settingsService?.GetLanguage() ?? "vi";
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    map.Pins.Clear();
                    _poiPins.Clear();
                    _poiPinDictionary.Clear();
                    
                    foreach (var poi in pois)
                    {
                        var pin = new Pin
                        {
                            Location = new Location(poi.Latitude, poi.Longitude),
                            Label = language == "en" ? poi.NameEn : poi.NameVi,
                            Address = poi.Address,
                            Type = PinType.Place
                        };
                        map.Pins.Add(pin);
                        _poiPins.Add(pin);
                        _poiPinDictionary[poi.Id] = pin;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadAllPOIsAsync] Error: {ex.Message}");
        }
    }

    // Auto sync POIs from Web Admin when app opens
    private async Task AutoSyncFromWebAsync()
    {
        try
        {
            var webAdminService = ServiceProviderHelper.GetService<IWebAdminService>();
            if (webAdminService == null)
            {
                System.Diagnostics.Debug.WriteLine("[AutoSync] WebAdminService not available");
                return;
            }

            // Check if auto-sync is enabled (default: true)
            var settingsService = ServiceProviderHelper.GetService<ISettingsService>();
            bool autoSyncEnabled = settingsService?.GetAutoSyncFromWeb() ?? true;
            
            if (!autoSyncEnabled)
            {
                System.Diagnostics.Debug.WriteLine("[AutoSync] Disabled by settings");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[AutoSync] Starting automatic sync from web...");
            
            // First test connection
            bool isConnected = await webAdminService.TestConnectionAsync();
            if (!isConnected)
            {
                System.Diagnostics.Debug.WriteLine("[AutoSync] Web Admin not reachable, skipping sync");
                return;
            }

            // Get remote POIs count before sync
            var remotePOIs = await webAdminService.GetAllPOIsAsync();
            if (remotePOIs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[AutoSync] No POIs on web, skipping sync");
                return;
            }

            // Get local count before sync
            var localPOIs = await _databaseService.GetPOIsAsync();
            int beforeCount = localPOIs.Count;

            // Perform sync (pull from web)
            await webAdminService.SyncFromWebAdminAsync();

            // Get new local count after sync
            localPOIs = await _databaseService.GetPOIsAsync();
            int afterCount = localPOIs.Count;
            int newPOIs = afterCount - beforeCount;

            System.Diagnostics.Debug.WriteLine($"[AutoSync] Completed: {remotePOIs.Count} remote POIs, {newPOIs} new local POIs");

            // Refresh UI if new POIs were added
            if (newPOIs > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DisplayPOIMarkers(localPOIs, _currentNearestPOI?.Id);
                    System.Diagnostics.Debug.WriteLine($"[AutoSync] UI refreshed with {afterCount} POIs");
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AutoSync] Error: {ex.Message}");
        }
    }

    // Helper method to get list of images for a POI
    private List<string> GetPOIImages(POI poi)
    {
        var images = new List<string>();
        
        // If POI has an Image property, add it as first image
        if (!string.IsNullOrEmpty(poi.Image))
        {
            images.Add(poi.Image);
        }
        else
        {
            images.Add("restaurant_placeholder.png");
        }
        
        // Add placeholder images for demo (in real app, these would come from POI.ImageList)
        // For now, add some variation based on POI ID
        var random = new Random(poi.Id);
        int additionalImages = random.Next(0, 4); // 0 to 3 additional images
        
        for (int i = 0; i < additionalImages; i++)
        {
            // Add different food images
            string[] foodImages = { "pho.png", "banhmi.png", "bunbo.png", "comtam.png", "banhxeo.png" };
            images.Add(foodImages[(poi.Id + i) % foodImages.Length]);
        }
        
        return images;
    }

    // Check POI opening hours and update status
    private void UpdatePOIStatus(POI poi, string language)
    {
        if (poiStatusLabel == null) return;
        
        bool isOpen = IsCurrentlyOpen(poi.OpeningHours);
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (isOpen)
            {
                poiStatusLabel.Text = language == "en" ? "Open" : "Đang mở cửa";
                poiStatusLabel.TextColor = Color.FromArgb("#4CAF50"); // Green
            }
            else
            {
                poiStatusLabel.Text = language == "en" ? "Closed" : "Đã đóng cửa";
                poiStatusLabel.TextColor = Color.FromArgb("#F44336"); // Red
            }
        });
    }

    // Parse opening hours and check if currently open
    private bool IsCurrentlyOpen(string? openingHours)
    {
        if (string.IsNullOrEmpty(openingHours)) return true; // Default to open if no data
        
        try
        {
            var now = DateTime.Now;
            var currentTime = now.TimeOfDay;
            var currentDay = now.DayOfWeek;
            
            // Parse format: "Mon-Fri: 07:00-22:00, Sat-Sun: 08:00-23:00"
            // or "07:00-22:00" for daily
            
            // Simple parsing for common format "HH:mm-HH:mm"
            if (openingHours.Contains("-"))
            {
                var parts = openingHours.Split('-');
                if (parts.Length == 2 && 
                    TimeSpan.TryParse(parts[0].Trim(), out var openTime) &&
                    TimeSpan.TryParse(parts[1].Trim(), out var closeTime))
                {
                    // Handle overnight hours (e.g., 18:00-02:00)
                    if (closeTime < openTime)
                    {
                        return currentTime >= openTime || currentTime <= closeTime;
                    }
                    return currentTime >= openTime && currentTime <= closeTime;
                }
            }
            
            return true; // Default to open if can't parse
        }
        catch
        {
            return true; // Default to open on error
        }
    }
    
    /// <summary>
    /// Calculate distance between two coordinates using Haversine formula
    /// </summary>
    private double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
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

    #region Expandable Bottom Sheet Handlers

    private void OnDragHandleTapped(object? sender, TappedEventArgs e)
    {
        ToggleBottomSheet();
    }

    private void OnBottomSheetPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Detect swipe direction
                if (e.TotalY < -50 && !_isBottomSheetExpanded)
                {
                    ExpandBottomSheet();
                }
                else if (e.TotalY > 50 && _isBottomSheetExpanded)
                {
                    CollapseBottomSheet();
                }
                break;
        }
    }

    private void ToggleBottomSheet()
    {
        if (_isBottomSheetExpanded)
            CollapseBottomSheet();
        else
            ExpandBottomSheet();
    }

    private void ExpandBottomSheet()
    {
        _isBottomSheetExpanded = true;
        
        System.Diagnostics.Debug.WriteLine("[MainPage] ===== EXPANDING BOTTOM SHEET =====");
        
        // Update height
        AbsoluteLayout.SetLayoutBounds(poiCard, new Rect(0, 1, 1, EXPANDED_HEIGHT));
        
        // Update indicator
        if (expandIndicator != null)
            expandIndicator.Text = "▼"; // Down arrow
        
        // Show close button
        if (closeButtonFrame != null)
            closeButtonFrame.IsVisible = true;
        
        // Load reviews when expanded
        if (_currentNearestPOI != null)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Loading reviews for POI {_currentNearestPOI.Id}: {_currentNearestPOI.NameVi}");
            _ = LoadReviewsForPOI(_currentNearestPOI.Id);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[MainPage] ERROR: _currentNearestPOI is null, cannot load reviews");
        }
        
        System.Diagnostics.Debug.WriteLine("[MainPage] ===== BOTTOM SHEET EXPANDED =====");
    }

    private void CollapseBottomSheet()
    {
        _isBottomSheetExpanded = false;
        
        // Update height
        AbsoluteLayout.SetLayoutBounds(poiCard, new Rect(0, 1, 1, COLLAPSED_HEIGHT));
        
        // Update indicator
        if (expandIndicator != null)
            expandIndicator.Text = "▲"; // Up arrow
        
        // Hide close button
        if (closeButtonFrame != null)
            closeButtonFrame.IsVisible = false;
        
        // Scroll to top
        if (poiScrollView != null)
            poiScrollView.ScrollToAsync(0, 0, false);
        
        System.Diagnostics.Debug.WriteLine("[MainPage] Bottom sheet COLLAPSED");
    }

    private void OnClosePOICardClicked(object? sender, TappedEventArgs e)
    {
        poiCard.IsVisible = false;
        CollapseBottomSheet();
    }

    #endregion

    #region Review Handlers

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
            if (_currentNearestPOI == null) return;
            if (_selectedRating == 0)
            {
                await DisplayAlert("Thông báo", "Vui lòng chọn số sao đánh giá!", "OK");
                return;
            }
            
            var deviceId = Preferences.Get("DeviceId", Guid.NewGuid().ToString());
            Preferences.Set("DeviceId", deviceId);
            
            var commentText = reviewCommentEditor?.Text ?? "";
            
            var review = new Review
            {
                POIId = _currentNearestPOI.Id,
                Rating = _selectedRating,
                Comment = commentText,
                UserId = deviceId,
                UserName = "Khách tham quan",
                CreatedAt = DateTime.Now
            };
            
            await _databaseService.AddReviewAsync(review);
            System.Diagnostics.Debug.WriteLine($"[MainPage] Review added locally: {_selectedRating} stars");
            
            // Hide form and reload
            OnCancelReviewClicked(sender, e);
            await LoadReviewsForPOI(_currentNearestPOI.Id);
            
            // Sync to Web Admin
            _ = Task.Run(async () =>
            {
                try
                {
                    var apiService = new ApiService();
                    var result = await apiService.PostReviewAsync(_currentNearestPOI!.Id, deviceId, "Khách tham quan", _selectedRating, commentText);
                    System.Diagnostics.Debug.WriteLine(result.Success 
                        ? "[MainPage] Review synced to Web Admin" 
                        : $"[MainPage] Failed to sync review: {result.Error}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainPage] Review sync error: {ex.Message}");
                }
            });
            
            await DisplayAlert("Cảm ơn!", "Đánh giá của bạn đã được ghi nhận!", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Error submitting review: {ex.Message}");
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
                stars[i].Text = i < _selectedRating ? "★" : "☆";
        }
    }

    private async Task LoadReviewsForPOI(int poiId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] START loading reviews for POI {poiId}");
            
            // Load local reviews
            var localReviews = await _databaseService.GetReviewsAsync(poiId);
            System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Found {localReviews.Count} local reviews");
            
            // Try fetch from Web Admin
            try
            {
                var apiService = new ApiService();
                var webResult = await apiService.GetReviewsAsync(poiId);
                
                System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Web API result: Success={webResult.Success}, Count={webResult.Data?.Count ?? 0}");
                
                if (webResult.Success && webResult.Data?.Data != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Web returned {webResult.Data.Data.Length} reviews");
                    
                    var webReviewIds = webResult.Data.Data.Select(r => r.Id).ToList();
                    
                    // Delete removed reviews
                    foreach (var localReview in localReviews.Where(r => !string.IsNullOrEmpty(r.WebReviewId)))
                    {
                        if (!webReviewIds.Contains(localReview.WebReviewId))
                        {
                            await _databaseService.DeleteReviewByWebIdAsync(localReview.WebReviewId);
                            System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Deleted removed review: {localReview.WebReviewId}");
                        }
                    }
                    
                    // Add new reviews
                    foreach (var webReview in webResult.Data.Data)
                    {
                        if (!localReviews.Any(r => r.WebReviewId == webReview.Id))
                        {
                            await _databaseService.AddReviewAsync(new Review
                            {
                                POIId = webReview.PoiId,
                                UserId = webReview.UserId ?? "",
                                UserName = webReview.UserName ?? "Khách tham quan",
                                Rating = webReview.Rating,
                                Comment = webReview.Comment ?? "",
                                CreatedAt = DateTime.TryParse(webReview.CreatedAt, out var date) ? date : DateTime.Now,
                                WebReviewId = webReview.Id,
                                LastSyncFromWeb = DateTime.Now
                            });
                            System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Added web review: {webReview.Id}, Rating={webReview.Rating}");
                        }
                    }
                    
                    localReviews = await _databaseService.GetReviewsAsync(poiId);
                    System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] After merge: {localReviews.Count} reviews");
                }
                else if (!webResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Web API failed: {webResult.Error}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Failed to fetch web reviews: {ex.Message}");
            }
            
            var reviews = localReviews;
            var averageRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
            System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Final: {reviews.Count} reviews, Avg={averageRating:F1}");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] UI Update - Rating={averageRating:F1}, Count={reviews.Count}");
                
                if (averageRatingLabel != null)
                    averageRatingLabel.Text = averageRating.ToString("F1");
                if (totalReviewsLabel != null)
                    totalReviewsLabel.Text = $"({reviews.Count} đánh giá)";
                
                if (reviewsContainer != null)
                {
                    reviewsContainer.Children.Clear();
                    System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Rendering {reviews.Count} reviews to UI");
                    
                    foreach (var review in reviews.Take(5))
                    {
                        var reviewFrame = new Frame
                        {
                            BackgroundColor = Color.FromArgb("#F7F7F7"),
                            CornerRadius = 8,
                            Padding = new Thickness(12),
                            BorderColor = Colors.Transparent
                        };
                        
                        var reviewLayout = new VerticalStackLayout { Spacing = 4 };
                        reviewLayout.Children.Add(new Label { Text = string.Join("", Enumerable.Repeat("⭐", review.Rating)), FontSize = 12 });
                        
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
                        
                        reviewLayout.Children.Add(new Label 
                        { 
                            Text = review.CreatedAt.ToString("dd/MM/yyyy"), 
                            FontSize = 12,
                            TextColor = Color.FromArgb("#8E8E93")
                        });
                        
                        reviewFrame.Content = reviewLayout;
                        reviewsContainer.Children.Add(reviewFrame);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[LoadReviewsForPOI] Finished rendering {reviewsContainer.Children.Count} review frames");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[LoadReviewsForPOI] ERROR: reviewsContainer is null!");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Error loading reviews: {ex.Message}");
        }
    }

    #endregion
}

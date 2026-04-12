using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using FoodStreetGuide.Services;
using FoodStreetGuide.Models;
using FoodStreetGuide.Platforms.Android;
using FoodStreetGuide.Database;

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
                
                // Enable geofence if not already enabled (e.g., after app resume from background)
                if (!_geofenceEngine.IsEnabled)
                {
                    var pois = await _databaseService.GetPOIsAsync();
                    System.Diagnostics.Debug.WriteLine($"[OnAppearing] Loaded {pois.Count} POIs from database");
                    
                    // Seed data if empty
                    if (pois.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[OnAppearing] Database empty, seeding test POIs");
                        var testPOIs = SeedData.GetTestPOIs();
                        foreach (var poi in testPOIs)
                        {
                            await _databaseService.AddPOIAsync(poi);
                        }
                        pois = await _databaseService.GetPOIsAsync();
                        System.Diagnostics.Debug.WriteLine($"[OnAppearing] Seeded {pois.Count} POIs");
                    }
                    
                    _geofenceEngine.SetPOIs(pois);
                    _geofenceEngine.Enable();
                    _geofenceEnabled = true;
                    
                    await Task.Delay(500);
                    DisplayPOIMarkers(pois);
                }
                else
                {
                    // Just refresh markers
                    var pois = await _databaseService.GetPOIsAsync();
                    System.Diagnostics.Debug.WriteLine($"[OnAppearing] Refreshed {pois.Count} POIs from database");
                    DisplayPOIMarkers(pois, _currentNearestPOI?.Id);
                }
                
                // Auto sync from web admin (non-blocking)
                _ = AutoSyncFromWebAsync();
            }
            
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
            
            // Resubscribe to location updates if tracking is active
            if (_locationService?.IsTracking == true && !_locationSubscribed)
            {
                SubscribeToLocationUpdates();
                System.Diagnostics.Debug.WriteLine("[OnAppearing] Resubscribed to location updates (tracking was active)");
            }
            
            // Request and display current location even if not tracking
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
        if (trackingIndicator == null || trackButton == null || trackButtonFrame == null) return;

        try
        {
            var language = _settingsService?.GetLanguage() ?? "vi";
            
            if (_locationSubscribed)
            {
                // STOP tracking state - Red #E63946
                trackingIndicator.IsVisible = true;
                if (trackingStatusLabel != null)
                    trackingStatusLabel.Text = language == "vi" ? "Đang theo dõi" : "Tracking Active";
                trackButtonFrame.BackgroundColor = Color.FromArgb("#E63946");  // Red color for Stop
                trackButton.TextColor = Colors.White;
                trackButton.Text = language == "vi" ? "Dừng" : "Stop";
            }
            else
            {
                // START tracking state - Orange color #FF6B35
                trackingIndicator.IsVisible = false;
                trackButtonFrame.BackgroundColor = Color.FromArgb("#FF6B35");
                trackButton.TextColor = Colors.White;
                trackButton.Text = language == "vi" ? "Theo dõi" : "Tracking";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateTrackingButtonState] Error: {ex.Message}");
        }
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
                    // Hide control bar when POI card is shown
                    if (controlBarFrame != null) controlBarFrame.IsVisible = false;
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
            if (controlBarFrame != null) controlBarFrame.IsVisible = false;
            
            var lang2 = _settingsService?.GetLanguage() ?? "vi";
            poiNameLabel.Text = lang2 == "en" ? e.POI.NameEn : e.POI.NameVi;
            poiDistanceLabel.Text = $"{e.DistanceMeters:F0}m";
            poiAddressLabel.Text = e.POI.Address;
            
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
                
                // Show control bar when POI card is hidden
                if (controlBarFrame != null) controlBarFrame.IsVisible = true;
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
            _currentNearestPOI = poi;
            
            // Show POI card with details
            poiCard.IsVisible = true;
            poiNameLabel.Text = poi.NameVi;
            poiDistanceLabel.Text = poi.Address ?? "Address not available";
            poiAddressLabel.Text = poi.Address ?? "No address information";
            
            // Update save heart color
            bool isSaved = _savedPOIService?.IsSaved(poi) ?? false;
            if (saveHeartLabel != null)
                saveHeartLabel.TextColor = isSaved ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            
            // Hide control bar to make room for card
            if (controlBarFrame != null)
                controlBarFrame.IsVisible = false;
            
            System.Diagnostics.Debug.WriteLine($"[ShowPOICardForMarker] Showing card for: {poi.NameVi}");
        });
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
        // Show control bar when POI card is closed
        if (controlBarFrame != null) controlBarFrame.IsVisible = true;
    }

    private void OnClosePOICardSwiped(object? sender, SwipedEventArgs e)
    {
        poiCard.IsVisible = false;
        // Show control bar when POI card is closed
        if (controlBarFrame != null) controlBarFrame.IsVisible = true;
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

    private async void OnDetailsClicked(object? sender, EventArgs e)
    {
        if (_currentNearestPOI == null) return;
        
        var detailPage = new ContentPage
        {
            BackgroundColor = Color.FromArgb("#80000000"),
            Padding = new Thickness(20)
        };

        var scrollView = new ScrollView
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };

        var card = new Frame
        {
            BackgroundColor = Colors.White,
            CornerRadius = 24,
            Padding = new Thickness(20),
            HasShadow = true,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill
        };

        var cardContent = new VerticalStackLayout { Spacing = 16 };

        if (!string.IsNullOrEmpty(_currentNearestPOI.Image))
        {
            cardContent.Children.Add(new Image
            {
                Source = _currentNearestPOI.Image,
                Aspect = Aspect.AspectFill,
                HeightRequest = 200,
                BackgroundColor = Colors.LightGray,
                Margin = new Thickness(-20, -20, -20, 0)
            });
        }

        cardContent.Children.Add(new Label
        {
            Text = _currentNearestPOI.NameVi,
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1C1C1E")
        });

        cardContent.Children.Add(new Label
        {
            Text = $"{_currentNearestPOI.Radius}m radius",
            FontSize = 14,
            TextColor = Color.FromArgb("#8E8E93")
        });

        cardContent.Children.Add(new Label
        {
            Text = _currentNearestPOI.DescriptionVi ?? "",
            FontSize = 15,
            TextColor = Color.FromArgb("#1C1C1E"),
            LineBreakMode = LineBreakMode.WordWrap
        });

        if (!string.IsNullOrEmpty(_currentNearestPOI.Address))
        {
            cardContent.Children.Add(new Frame
            {
                BackgroundColor = Color.FromArgb("#F2F2F7"),
                CornerRadius = 12,
                Padding = new Thickness(12),
                Content = new Label
                {
                    Text = _currentNearestPOI.Address,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#1C1C1E")
                }
            });
        }

        if (!string.IsNullOrEmpty(_currentNearestPOI.OpeningHours))
        {
            var hoursFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#34C759").WithAlpha(0.1f),
                CornerRadius = 12,
                Padding = new Thickness(12),
                Content = new Label
                {
                    Text = "Open: " + _currentNearestPOI.OpeningHours,
                    FontSize = 14,
                    TextColor = Color.FromArgb("#34C759"),
                    FontAttributes = FontAttributes.Bold
                }
            };
            cardContent.Children.Add(hoursFrame);
        }

        var closeButton = new Button
        {
            Text = "Close",
            BackgroundColor = Color.FromArgb("#1C1C1E"),
            TextColor = Colors.White,
            CornerRadius = 12,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 16, 0, 0)
        };
        closeButton.Clicked += async (s, args) => await Navigation.PopModalAsync();
        cardContent.Children.Add(closeButton);

        card.Content = cardContent;
        scrollView.Content = card;
        detailPage.Content = scrollView;

        await Navigation.PushModalAsync(detailPage);
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
        
        // Control bar buttons
        if (trackButton != null)
            trackButton.Text = _locationSubscribed ? loc.GetString("Stop") : loc.GetString("Tracking");
        if (nearestButton != null)
            nearestButton.Text = loc.GetString("Nearest");
        if (qrButton != null)
            qrButton.Text = loc.GetString("QR");
        
        // POI card labels
        if (listenLabel != null)
            listenLabel.Text = loc.GetString("POI_Listen");
        if (directionsLabel != null)
            directionsLabel.Text = loc.GetString("POI_Directions");
        if (detailsLabel != null)
            detailsLabel.Text = loc.GetString("POI_Details");
        
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
            if (controlBarFrame != null) controlBarFrame.IsVisible = false;
            
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
}

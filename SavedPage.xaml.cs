using FoodStreetGuide.Models;
using FoodStreetGuide.Services;

namespace FoodStreetGuide;

public partial class SavedPage : ContentPage
{
    private readonly ILocalizationService? _localizationService;
    private readonly ISavedPOIService? _savedPOIService;

    public SavedPage()
    {
        InitializeComponent();
        _localizationService = ServiceProviderHelper.GetService<ILocalizationService>();
        _savedPOIService = ServiceProviderHelper.GetService<ISavedPOIService>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateLanguage();
        UpdateSavedList();
        
        // Subscribe to changes
        if (_localizationService != null)
            _localizationService.LanguageChanged += OnLanguageChanged;
        if (_savedPOIService != null)
            _savedPOIService.SavedPOIsChanged += OnSavedPOIsChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Unsubscribe from changes
        if (_localizationService != null)
            _localizationService.LanguageChanged -= OnLanguageChanged;
        if (_savedPOIService != null)
            _savedPOIService.SavedPOIsChanged -= OnSavedPOIsChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLanguage();
    }

    private void OnSavedPOIsChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateSavedList);
    }

    private void UpdateLanguage()
    {
        if (_localizationService == null) return;
        var loc = _localizationService;
        
        // Page title (shown in tab bar)
        Title = loc.GetString("Tab_Saved");
        
        // Header
        if (headerTitleLabel != null)
            headerTitleLabel.Text = loc.GetString("Saved_Title");
        if (headerSubtitleLabel != null)
            headerSubtitleLabel.Text = loc.GetString("Saved_Subtitle");
        
        // Empty state
        if (emptyTitleLabel != null)
            emptyTitleLabel.Text = loc.GetString("Saved_EmptyTitle");
        if (emptySubtitleLabel != null)
            emptySubtitleLabel.Text = loc.GetString("Saved_EmptySubtitle");
        if (exploreButton != null)
            exploreButton.Text = loc.GetString("Saved_ExploreButton");
    }

    private void UpdateSavedList()
    {
        try
        {
            if (_savedPOIService == null) return;
            
            var savedPOIs = _savedPOIService.SavedPOIs.ToList();
            System.Diagnostics.Debug.WriteLine($"[SavedPage] Updating list with {savedPOIs.Count} items");
            
            // Update visibility of items
            savedItem1.IsVisible = savedPOIs.Count > 0;
            savedItem2.IsVisible = savedPOIs.Count > 1;
            savedItem3.IsVisible = savedPOIs.Count > 2;
            
            // Update empty state
            emptyStateLayout.IsVisible = savedPOIs.Count == 0;
            
            // Update item 1
            if (savedPOIs.Count > 0) UpdateItemContent(0, savedPOIs[0]);
            if (savedPOIs.Count > 1) UpdateItemContent(1, savedPOIs[1]);
            if (savedPOIs.Count > 2) UpdateItemContent(2, savedPOIs[2]);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateSavedList] Error: {ex.Message}");
        }
    }

    private void UpdateItemContent(int index, POI poi)
    {
        var names = new[] { savedName1, savedName2, savedName3 };
        var ratings = new[] { savedRating1, savedRating2, savedRating3 };
        var distances = new[] { savedDistance1, savedDistance2, savedDistance3 };
        
        if (index < names.Length && names[index] != null)
            names[index].Text = poi.NameVi;
        if (index < ratings.Length && ratings[index] != null)
            ratings[index].Text = "4.8"; // TODO: Get actual rating
        if (index < distances.Length && distances[index] != null)
            distances[index].Text = "500m"; // TODO: Calculate actual distance
    }

    // Tap on saved item → open POI Detail
    private async void OnSavedItemTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var tapGesture = sender as TapGestureRecognizer;
            var indexStr = tapGesture?.CommandParameter as string ?? "0";
            var index = int.Parse(indexStr);
            
            System.Diagnostics.Debug.WriteLine($"[SavedPage] Item {index} tapped");
            
            if (_savedPOIService == null) return;
            var savedPOIs = _savedPOIService.SavedPOIs.ToList();
            
            if (index < savedPOIs.Count)
            {
                var poi = savedPOIs[index];
                await Navigation.PushAsync(new POIDetailPage(poi));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnSavedItemTapped] Error: {ex.Message}");
        }
    }

    // Tap on heart → remove from favorites
    private async void OnRemoveFavoriteTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var tapGesture = sender as TapGestureRecognizer;
            var indexStr = tapGesture?.CommandParameter as string ?? "0";
            var index = int.Parse(indexStr);
            
            System.Diagnostics.Debug.WriteLine($"[SavedPage] Remove favorite {index}");
            
            if (_savedPOIService == null) return;
            var savedPOIs = _savedPOIService.SavedPOIs.ToList();
            
            if (index < savedPOIs.Count)
            {
                var poi = savedPOIs[index];
                
                // Animate removal
                await AnimateItemRemoval(index);
                
                // Actually remove from service
                _savedPOIService.Unsave(poi);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnRemoveFavoriteTapped] Error: {ex.Message}");
        }
    }

    private async Task AnimateItemRemoval(int index)
    {
        var frames = new[] { savedItem1, savedItem2, savedItem3 };
        if (index < frames.Length && frames[index] != null)
        {
            var frame = frames[index];
            await frame.FadeTo(0, 300);
            await frame.ScaleTo(0.9, 200);
            System.Diagnostics.Debug.WriteLine($"[AnimateItemRemoval] Item {index} animated");
        }
    }

    // Empty state button → go to Discover tab
    private void OnExploreClicked(object? sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[SavedPage] Explore button clicked");
            if (Shell.Current != null)
                Shell.Current.GoToAsync("//DiscoverPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnExploreClicked] Error: {ex.Message}");
        }
    }
}

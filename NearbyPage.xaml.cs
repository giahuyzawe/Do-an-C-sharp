using FoodStreetGuide.Models;
using FoodStreetGuide.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;

namespace FoodStreetGuide;

public partial class NearbyPage : ContentPage
{
    private readonly IDatabaseService _databaseService;
    private List<POI> _allPOIs = new();

    public NearbyPage(IDatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
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
            _allPOIs = await _databaseService.GetPOIsAsync();
            
            // Try to get current location to calculate distances
            try
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(5)
                });

                if (location != null)
                {
                    // Calculate distances
                    foreach (var poi in _allPOIs)
                    {
                        var poiLocation = new Location(poi.Latitude, poi.Longitude);
                        var distance = location.CalculateDistance(poiLocation, DistanceUnits.Kilometers);
                        poi.DistanceText = distance < 1 ? $"{distance * 1000:F0}m" : $"{distance:F1}km";
                    }

                    // Sort by distance
                    _allPOIs = _allPOIs.OrderBy(p => 
                    {
                        var poiLoc = new Location(p.Latitude, p.Longitude);
                        return location.CalculateDistance(poiLoc, DistanceUnits.Kilometers);
                    }).ToList();
                }
            }
            catch
            {
                // If location not available, show all without distance
                foreach (var poi in _allPOIs)
                {
                    poi.DistanceText = "";
                }
            }

            poiCollectionView.ItemsSource = _allPOIs;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NearbyPage] Error loading POIs: {ex.Message}");
        }
    }

    private async void OnPOISelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is POI selectedPOI)
        {
            // Navigate to map with this POI selected
            await Shell.Current.GoToAsync($"//MainPage?poiId={selectedPOI.Id}");
            
            // Clear selection
            poiCollectionView.SelectedItem = null;
        }
    }
}

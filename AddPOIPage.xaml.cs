using FoodStreetGuide.Models;
using FoodStreetGuide.Services;

namespace FoodStreetGuide;

public partial class AddPOIPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public AddPOIPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(nameViEntry.Text))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập tên quán", "OK");
                return;
            }

            if (!double.TryParse(latEntry.Text, out double lat))
            {
                await DisplayAlert("Lỗi", "Vĩ độ không hợp lệ", "OK");
                return;
            }

            if (!double.TryParse(lngEntry.Text, out double lng))
            {
                await DisplayAlert("Lỗi", "Kinh độ không hợp lệ", "OK");
                return;
            }

            if (!double.TryParse(radiusEntry.Text, out double radius))
            {
                radius = 50;
            }

            if (!int.TryParse(priorityEntry.Text, out int priority))
            {
                priority = 1;
            }

            var poi = new POI
            {
                NameVi = nameViEntry.Text?.Trim() ?? "",
                NameEn = nameEnEntry.Text?.Trim() ?? "",
                DescriptionVi = descViEditor.Text?.Trim() ?? "",
                DescriptionEn = descEnEditor.Text?.Trim() ?? "",
                Latitude = lat,
                Longitude = lng,
                Radius = radius,
                Priority = priority,
                Image = imageEntry.Text?.Trim() ?? "",
                AudioVi = audioViEntry.Text?.Trim() ?? "",
                AudioEn = audioEnEntry.Text?.Trim() ?? "",
                MapUrl = mapUrlEntry.Text?.Trim() ?? ""
            };

            await _databaseService.AddPOIAsync(poi);

            await DisplayAlert("Thành công", "Đã thêm quán ăn!", "OK");
            
            // Clear form
            nameViEntry.Text = "";
            nameEnEntry.Text = "";
            descViEditor.Text = "";
            descEnEditor.Text = "";
            latEntry.Text = "";
            lngEntry.Text = "";
            radiusEntry.Text = "50";
            priorityEntry.Text = "1";
            imageEntry.Text = "";
            audioViEntry.Text = "";
            audioEnEntry.Text = "";
            mapUrlEntry.Text = "";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể thêm POI: {ex.Message}", "OK");
        }
    }

    private async void OnGetCurrentLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            
            if (location == null)
            {
                location = await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium));
            }

            if (location != null)
            {
                latEntry.Text = location.Latitude.ToString();
                lngEntry.Text = location.Longitude.ToString();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể lấy vị trí: {ex.Message}", "OK");
        }
    }
}

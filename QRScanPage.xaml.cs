using FoodStreetGuide.Services;
using FoodStreetGuide.Models;
using CommunityToolkit.Mvvm.Messaging;

namespace FoodStreetGuide;

public partial class QRScanPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly string _deviceId;
    private bool _isProcessing = false;

    public QRScanPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        
        // Get or create device ID for tracking
        _deviceId = GetDeviceId();
    }

    private string GetDeviceId()
    {
        // Try to get existing device ID
        var existingId = Preferences.Get("DeviceId", string.Empty);
        if (!string.IsNullOrEmpty(existingId))
        {
            return existingId;
        }

        // Generate new device ID
        var newId = Guid.NewGuid().ToString();
        Preferences.Set("DeviceId", newId);
        return newId;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnManualEntryCompleted(object sender, EventArgs e)
    {
        ProcessQRCode(manualEntry?.Text);
    }

    private void OnManualEntryClicked(object sender, EventArgs e)
    {
        ProcessQRCode(manualEntry?.Text);
    }

    private async void ProcessQRCode(string? code)
    {
        if (_isProcessing) return;
        
        if (string.IsNullOrWhiteSpace(code))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập mã QR", "OK");
            return;
        }

        _isProcessing = true;
        code = code.Trim();

        try
        {
            // Extract POI ID from QR code
            int? poiId = ExtractPoiId(code);
            
            if (!poiId.HasValue)
            {
                await DisplayAlert("Mã không hợp lệ", 
                    "Mã QR không đúng định dạng FoodStreetGuide.\n\nCác định dạng hỗ trợ:\n" +
                    "• foodstreetguide://poi/123\n" +
                    "• https://.../poi/123\n" +
                    "• ID trực tiếp: 123", "OK");
                _isProcessing = false;
                return;
            }

            // Check for duplicate scan with cooldown
            var (isDuplicate, timeRemaining) = await _databaseService.RecordQRScanAsync(
                poiId.Value, _deviceId, code);

            if (isDuplicate)
            {
                // Show duplicate warning but still allow navigation
                var minutesRemaining = (int)Math.Ceiling(timeRemaining?.TotalMinutes ?? 60);
                var navigateAnyway = await DisplayAlert("Quét trùng", 
                    $"Bạn đã quét quán này cách đây chưa đầy 1 giờ.\n\n" +
                    $"Thời gian còn lại: {minutesRemaining} phút\n\n" +
                    $"Quét trùng sẽ KHÔNG tính thêm lượt xem.",
                    "Vẫn đi đến", "Hủy");

                if (navigateAnyway)
                {
                    await NavigateToPOI(poiId.Value, countedAsVisit: false);
                }
                _isProcessing = false;
                return;
            }

            // New scan - show check-in success and navigate
            await DisplayAlert("Check-in thành công!", 
                "Cảm ơn bạn đã đến thăm quán này.", "OK");
            
            await NavigateToPOI(poiId.Value, countedAsVisit: true);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể xử lý mã QR: {ex.Message}", "OK");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private int? ExtractPoiId(string code)
    {
        // Deep link: foodstreetguide://poi/123
        if (code.StartsWith("foodstreetguide://poi/"))
        {
            var parts = code.Split('/');
            if (parts.Length >= 4 && int.TryParse(parts[3], out int id1))
            {
                return id1;
            }
        }

        // Web URL: https://.../poi/123
        if (code.Contains("/poi/"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(code, @"/poi/(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id2))
            {
                return id2;
            }
        }

        // Direct ID
        if (int.TryParse(code, out int directId))
        {
            return directId;
        }

        return null;
    }

    private async Task NavigateToPOI(int poiId, bool countedAsVisit)
    {
        // Get POI from database
        var pois = await _databaseService.GetPOIsAsync();
        var poi = pois.FirstOrDefault(p => p.Id == poiId);

        if (poi == null)
        {
            await DisplayAlert("Không tìm thấy", 
                "Quán ăn này chưa có trong hệ thống hoặc đã bị xóa.", "OK");
            _isProcessing = false;
            return;
        }

        // Check if POI is approved
        if (poi.ApprovalStatus != "approved")
        {
            await DisplayAlert("Chưa được duyệt", 
                "Quán này đang chờ được duyệt. Vui lòng quay lại sau.", "OK");
            _isProcessing = false;
            return;
        }

        // Navigate back and show POI
        await Navigation.PopAsync();

        // Send message to MainPage to focus on this POI
        WeakReferenceMessenger.Default.Send(new QRCodeScannedMessage(poi, countedAsVisit));
    }
}

// Message class for communication between pages
public class QRCodeScannedMessage
{
    public POI POI { get; }
    public bool CountedAsVisit { get; }

    public QRCodeScannedMessage(POI poi, bool countedAsVisit)
    {
        POI = poi;
        CountedAsVisit = countedAsVisit;
    }
}

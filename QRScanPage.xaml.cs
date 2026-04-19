using FoodStreetGuide.Services;
using FoodStreetGuide.Models;
// using CommunityToolkit.Mvvm.Messaging; // Temporarily removed for build

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
            // Check if it's a dynamic QR code (foodstreetguide://qr/TOKEN)
            if (code.StartsWith("foodstreetguide://qr/"))
            {
                await ProcessDynamicQR(code);
                return;
            }

            // Legacy support: Direct POI ID or poi/ID format
            int? poiId = ExtractPoiId(code);
            
            if (!poiId.HasValue)
            {
                await DisplayAlert("Mã không hợp lệ", 
                    "Mã QR không đúng định dạng FoodStreetGuide.\n\nCác định dạng hỗ trợ:\n" +
                    "• foodstreetguide://qr/abc123 (QR động)\n" +
                    "• foodstreetguide://poi/123\n" +
                    "• ID trực tiếp: 123", "OK");
                _isProcessing = false;
                return;
            }

            // Process legacy QR (direct POI reference)
            await ProcessLegacyQR(poiId.Value, code);
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

    /// <summary>
    /// Process dynamic QR code with unique token
    /// </summary>
    private async Task ProcessDynamicQR(string code)
    {
        // Extract token
        var token = code.Replace("foodstreetguide://qr/", "").Trim();
        
        if (string.IsNullOrEmpty(token))
        {
            await DisplayAlert("Lỗi", "Mã QR không hợp lệ (thiếu token)", "OK");
            _isProcessing = false;
            return;
        }

        // Validate dynamic QR code via Web Admin API
        ApiService apiService = new ApiService();
        var qrResult = await apiService.CheckQRAsync(token, _deviceId);
        
        if (!qrResult.Success)
        {
            await DisplayAlert("QR không hợp lệ", qrResult.Error ?? "Mã QR không thể sử dụng", "OK");
            _isProcessing = false;
            return;
        }

        // Get POI from local database
        var poi = await _databaseService.GetPOIAsync(qrResult.Data.PoiId);
        if (poi == null)
        {
            await DisplayAlert("Lỗi", "Không tìm thấy thông tin nhà hàng", "OK");
            _isProcessing = false;
            return;
        }

        // New scan - show success
        await DisplayAlert("Check-in thành công!", 
            $"Cảm ơn bạn đã đến thăm:\n{poi.NameVi}\n\nBạn là khách thứ {qrResult.Data.CheckInNumber} check-in tại đây!", "OK");
        
        // Record check-in for analytics (local + API)
        try
        {
            await _databaseService.RecordCheckInAsync(poi.Id, _deviceId, token);
            
            // Send to Web Admin API
            var analyticsResult = await apiService.PostAnalyticsAsync("check_in", _deviceId, poi.Id, token);
            if (analyticsResult.Success)
            {
                System.Diagnostics.Debug.WriteLine($"[QRScanPage] Check-in sent to Web Admin: {poi.Id}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QRScanPage] Failed to record check-in: {ex.Message}");
        }
        
        await NavigateToPOI(poi, countedAsVisit: true, isDynamicQR: true);
    }

    /// <summary>
    /// Process legacy QR code (direct POI ID)
    /// </summary>
    private async Task ProcessLegacyQR(int poiId, string code)
    {
        // Check for duplicate scan with cooldown
        var (isDuplicate, timeRemaining) = await _databaseService.RecordQRScanAsync(
            poiId, _deviceId, code);

        if (isDuplicate)
        {
            var minutesRemaining = (int)Math.Ceiling(timeRemaining?.TotalMinutes ?? 60);
            var navigateAnyway = await DisplayAlert("Quét trùng", 
                $"Bạn đã quét quán này cách đây chưa đầy 1 giờ.\n\n" +
                $"Thời gian còn lại: {minutesRemaining} phút\n\n" +
                $"Quét trùng sẽ KHÔNG tính thêm lượt xem.",
                "Vẫn đi đến", "Hủy");

            if (navigateAnyway)
            {
                await NavigateToPOI(poiId, countedAsVisit: false);
            }
            _isProcessing = false;
            return;
        }

        // New scan
        await DisplayAlert("Check-in thành công!", 
            "Cảm ơn bạn đã đến thăm quán này.", "OK");
        
        // Record check-in for analytics
        try
        {
            await _databaseService.RecordCheckInAsync(poiId, _deviceId, code);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QRScanPage] Failed to record check-in: {ex.Message}");
        }
        
        await NavigateToPOI(poiId, countedAsVisit: true);
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

    /// <summary>
    /// Navigate to POI by ID (legacy support)
    /// </summary>
    private async Task NavigateToPOI(int poiId, bool countedAsVisit)
    {
        var pois = await _databaseService.GetPOIsAsync();
        var poi = pois.FirstOrDefault(p => p.Id == poiId);

        if (poi == null)
        {
            await DisplayAlert("Không tìm thấy", 
                "Quán ăn này chưa có trong hệ thống hoặc đã bị xóa.", "OK");
            _isProcessing = false;
            return;
        }

        await NavigateToPOI(poi, countedAsVisit, isDynamicQR: false);
    }

    /// <summary>
    /// Navigate to POI object
    /// </summary>
    private async Task NavigateToPOI(POI poi, bool countedAsVisit, bool isDynamicQR)
    {
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
        // TODO: Re-enable when CommunityToolkit.Mvvm is installed
        // WeakReferenceMessenger.Default.Send(new QRCodeScannedMessage(poi, countedAsVisit, isDynamicQR));
    }
}

// Message class for communication between pages
// TODO: Re-enable when CommunityToolkit.Mvvm is installed
/*
public class QRCodeScannedMessage
{
    public POI POI { get; }
    public bool CountedAsVisit { get; }
    public bool IsDynamicQR { get; }

    public QRCodeScannedMessage(POI poi, bool countedAsVisit, bool isDynamicQR)
    {
        POI = poi;
        CountedAsVisit = countedAsVisit;
        IsDynamicQR = isDynamicQR;
    }
}
*/

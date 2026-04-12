namespace FoodStreetGuide;

public partial class QRScanPage : ContentPage
{
    public QRScanPage()
    {
        InitializeComponent();
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
        if (string.IsNullOrWhiteSpace(code))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập mã QR", "OK");
            return;
        }

        // Process the QR code - could be a restaurant ID, URL, or promo code
        code = code.Trim();
        
        await DisplayAlert("Mã QR đã quét", $"Nội dung: {code}\n\nĐang xử lý...", "OK");
        
        // TODO: Implement actual QR processing logic
        // - If it's a restaurant ID, navigate to that POI
        // - If it's a URL, open it
        // - If it's a promo code, apply it
        
        // Example: Navigate back and show the POI
        await Navigation.PopAsync();
    }
}

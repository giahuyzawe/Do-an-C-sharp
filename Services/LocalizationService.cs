namespace FoodStreetGuide.Services;

public interface ILocalizationService
{
    string GetString(string key);
    void SetLanguage(string language); // "vi" or "en"
    string CurrentLanguage { get; }
    event EventHandler? LanguageChanged;
}

public class LocalizationService : ILocalizationService
{
    private readonly ISettingsService _settingsService;
    private Dictionary<string, Dictionary<string, string>> _resources;
    
    public string CurrentLanguage { get; private set; } = "vi";
    
    public event EventHandler? LanguageChanged;
    
    public LocalizationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        CurrentLanguage = _settingsService.GetLanguage();
        LoadResources();
    }
    
    public void SetLanguage(string language)
    {
        if (language != "vi" && language != "en") return;
        
        CurrentLanguage = language;
        _settingsService.SetLanguage(language);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public string GetString(string key)
    {
        if (_resources.TryGetValue(CurrentLanguage, out var dict))
        {
            if (dict.TryGetValue(key, out var value))
                return value;
        }
        return key; // Return key if not found
    }
    
    private void LoadResources()
    {
        _resources = new Dictionary<string, Dictionary<string, string>>
        {
            ["vi"] = new Dictionary<string, string>
            {
                // MainPage
                ["SearchPlaceholder"] = "Tìm nhà hàng hoặc món ăn...",
                ["TrackingActive"] = "Đang theo dõi",
                ["Tracking"] = "Theo dõi",
                ["Stop"] = "Dừng",
                ["Nearest"] = "Gần nhất",
                ["QR"] = "QR",
                
                // POI Card
                ["POI_Listen"] = "Nghe",
                ["POI_Directions"] = "Chỉ đường",
                ["POI_Details"] = "Chi tiết",
                ["POI_Save"] = "Lưu",
                ["POI_Open"] = "Đang mở",
                ["POI_Open24h"] = "Mở 24h",
                ["POI_Closed"] = "Đã đóng",
                ["POI_DefaultName"] = "Tên nhà hàng",
                ["POI_DefaultAddress"] = "Địa chỉ nhà hàng",
                
                // Tabs
                ["Tab_Map"] = "Bản đồ",
                ["Tab_Discover"] = "Khám phá",
                ["Tab_Saved"] = "Đã lưu",
                ["Tab_Settings"] = "Cài đặt",
                
                // Discover Page
                ["Discover_Title"] = "Khám phá",
                ["Discover_Subtitle"] = "Tìm nhà hàng ngon gần bạn",
                ["Discover_Distance"] = "{0}m",
                ["Discover_DistanceKm"] = "{0:F1}km",
                
                // Filter
                ["Filter_Popular"] = "Phổ biến",
                ["Filter_Distance"] = "Gần nhất",
                ["Filter_Food"] = "Món Việt",
                
                // Saved Page
                ["Saved_Title"] = "Đã lưu",
                ["Saved_Subtitle"] = "Nhà hàng yêu thích của bạn",
                ["Saved_Empty"] = "Bạn chưa lưu nhà hàng nào",
                ["Saved_EmptyHint"] = "Nhấn vào biểu tượng trái tim để lưu nhà hàng",
                
                // Settings Page
                ["Settings_Title"] = "Cài đặt",
                ["Settings_Profile"] = "Người dùng FoodStreetGuide",
                ["Settings_ProfileSubtitle"] = "Khám phá nhà hàng gần bạn",
                ["Settings_GeofenceSection"] = "PHẠM VI PHÁT HIỆN",
                ["Settings_Radius"] = "Bán kính phát hiện",
                ["Settings_Cooldown"] = "Thời gian chờ",
                ["Settings_Minutes"] = "{0} phút",
                ["Settings_NarrationSection"] = "THUYẾT MINH",
                ["Settings_EnableNarration"] = "Bật thuyết minh",
                ["Settings_Language"] = "Ngôn ngữ",
                ["Settings_SpeechSpeed"] = "Tốc độ nói",
                ["Settings_GPSSection"] = "GPS",
                ["Settings_UpdateInterval"] = "Khoảng thời gian cập nhật",
                ["Settings_Seconds"] = "{0} giây",
                ["Settings_Sync"] = "Đồng bộ dữ liệu",
                ["Settings_AboutSection"] = "GIỚI THIỆU",
                ["Settings_Version"] = "Phiên bản",
                ["Settings_Developer"] = "Nhà phát triển",
                ["Settings_LanguageVI"] = "Tiếng Việt",
                ["Settings_LanguageEN"] = "English",
                
                // Audio/Alerts
                ["Alert_Saved"] = "Đã lưu {0}",
                ["Alert_Unsaved"] = "Đã bỏ lưu {0}",
                ["Alert_NoPOI"] = "Không có địa điểm để lưu",
                ["Alert_NoNearbyPOI"] = "Không có địa điểm gần đây",
                ["Alert_Notification"] = "Thông báo",
                ["Alert_Error"] = "Lỗi",
            },
            ["en"] = new Dictionary<string, string>
            {
                // MainPage
                ["SearchPlaceholder"] = "Search restaurants or food...",
                ["TrackingActive"] = "Tracking Active",
                ["Tracking"] = "Tracking",
                ["Stop"] = "Stop",
                ["Nearest"] = "Nearest",
                ["QR"] = "QR",
                
                // POI Card
                ["POI_Listen"] = "Listen",
                ["POI_Directions"] = "Directions",
                ["POI_Details"] = "Details",
                ["POI_Save"] = "Save",
                ["POI_Open"] = "Open now",
                ["POI_Open24h"] = "Open 24h",
                ["POI_Closed"] = "Closed",
                ["POI_DefaultName"] = "Restaurant Name",
                ["POI_DefaultAddress"] = "Restaurant Address",
                
                // Tabs
                ["Tab_Map"] = "Map",
                ["Tab_Discover"] = "Discover",
                ["Tab_Saved"] = "Saved",
                ["Tab_Settings"] = "Settings",
                
                // Discover Page
                ["Discover_Title"] = "Discover",
                ["Discover_Subtitle"] = "Find the best restaurants near you",
                ["Discover_Distance"] = "{0}m",
                ["Discover_DistanceKm"] = "{0:F1}km",
                
                // Filter
                ["Filter_Popular"] = "Popular",
                ["Filter_Distance"] = "Nearest",
                ["Filter_Food"] = "Vietnamese",
                
                // Saved Page
                ["Saved_Title"] = "Saved",
                ["Saved_Subtitle"] = "Your favorite restaurants",
                ["Saved_Empty"] = "You have no saved restaurants",
                ["Saved_EmptyHint"] = "Tap the heart icon to save a restaurant",
                
                // Settings Page
                ["Settings_Title"] = "Settings",
                ["Settings_Profile"] = "FoodStreetGuide User",
                ["Settings_ProfileSubtitle"] = "Explore nearby restaurants",
                ["Settings_GeofenceSection"] = "GEOFENCE",
                ["Settings_Radius"] = "Detection radius",
                ["Settings_Cooldown"] = "Cooldown",
                ["Settings_Minutes"] = "{0} min",
                ["Settings_NarrationSection"] = "NARRATION",
                ["Settings_EnableNarration"] = "Enable narration",
                ["Settings_Language"] = "Language",
                ["Settings_SpeechSpeed"] = "Speech speed",
                ["Settings_GPSSection"] = "GPS",
                ["Settings_UpdateInterval"] = "Update interval",
                ["Settings_Seconds"] = "{0} sec",
                ["Settings_Sync"] = "Sync Data",
                ["Settings_AboutSection"] = "ABOUT",
                ["Settings_Version"] = "App version",
                ["Settings_Developer"] = "Developer",
                ["Settings_LanguageVI"] = "Tiếng Việt",
                ["Settings_LanguageEN"] = "English",
                
                // Audio/Alerts
                ["Alert_Saved"] = "Saved {0}",
                ["Alert_Unsaved"] = "Unsaved {0}",
                ["Alert_NoPOI"] = "No POI to save",
                ["Alert_NoNearbyPOI"] = "No nearby POI",
                ["Alert_Notification"] = "Notification",
                ["Alert_Error"] = "Error",
            }
        };
    }
}

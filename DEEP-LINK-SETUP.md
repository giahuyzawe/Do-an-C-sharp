# 🔗 Hướng dẫn: QR Code mở thẳng FoodStreetGuide App

## 🎯 Mục tiêu
Quét QR bằng camera điện thoại → Hiện popup "Mở bằng FoodStreetGuide" → Vô thẳng app

---

## 📱 Bước 1: Cấu hình App nhận Deep Link

### 1.1 Android - `Platforms/Android/AndroidManifest.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    
    <application 
        android:allowBackup="true" 
        android:icon="@mipmap/appicon" 
        android:roundIcon="@mipmap/appicon_round" 
        android:supportsRtl="true">
        
        <activity 
            android:name="mainactivity" 
            android:exported="true"
            android:launchMode="singleTask">
            
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
            
            <!-- 🔥 Deep Link: foodstreetguide://poi/123 -->
            <intent-filter>
                <action android:name="android.intent.action.VIEW" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.BROWSABLE" />
                <data android:scheme="foodstreetguide" 
                      android:host="poi" />
            </intent-filter>
            
            <!-- 🔥 HTTPS Universal Link (tùy chọn) -->
            <intent-filter android:autoVerify="true">
                <action android:name="android.intent.action.VIEW" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.BROWSABLE" />
                <data android:scheme="https" 
                      android:host="foodstreetguide.app" 
                      android:pathPrefix="/poi/" />
            </intent-filter>
            
        </activity>
    </application>
    
    <!-- Permissions -->
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
    <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
    <uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
    
</manifest>
```

### 1.2 iOS - `Platforms/iOS/Info.plist`

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Các key mặc định -->
    <key>CFBundleDisplayName</key>
    <string>FoodStreetGuide</string>
    <key>CFBundleIdentifier</key>
    <string>com.companyname.foodstreetguide</string>
    
    <!-- 🔥 URL Scheme để nhận deep link -->
    <key>CFBundleURLTypes</key>
    <array>
        <dict>
            <key>CFBundleURLName</key>
            <string>com.companyname.foodstreetguide.poi</string>
            <key>CFBundleURLSchemes</key>
            <array>
                <string>foodstreetguide</string>
            </array>
        </dict>
    </array>
    
    <!-- Location permissions -->
    <key>NSLocationWhenInUseUsageDescription</key>
    <string>App cần vị trí để tìm quán ăn gần bạn</string>
    <key>NSLocationAlwaysUsageDescription</key>
    <string>App cần vị trí để thông báo khi gần quán ăn</string>
    
</dict>
</plist>
```

---

## 💻 Bước 2: Code xử lý Deep Link trong App

### 2.1 Tạo `App.xaml.cs` - Nhận URL khi app mở

```csharp
using System.Diagnostics;

namespace FoodStreetGuide;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
    
    protected override void OnStart()
    {
        base.OnStart();
        // Xử lý deep link khi app cold start
        HandleDeepLink();
    }
    
    protected override void OnResume()
    {
        base.OnResume();
        // Xử lý deep link khi app resume
        HandleDeepLink();
    }
    
    private void HandleDeepLink()
    {
        // Lấy URI nếu app được mở từ deep link
        var uri = Microsoft.Maui.ApplicationModel.AppAction?
            .Current?.ToString();
            
        if (string.IsNullOrEmpty(uri))
            return;
            
        Debug.WriteLine($"Deep Link received: {uri}");
        
        // Parse và xử lý
        DeepLinkHandler.ProcessUri(uri);
    }
}
```

### 2.2 Tạo `DeepLinkHandler.cs` - Xử lý logic

```csharp
using System.Diagnostics;
using System.Web;
using FoodStreetGuide.Models;

namespace FoodStreetGuide;

public static class DeepLinkHandler
{
    public static void ProcessUri(string uriString)
    {
        try
        {
            var uri = new Uri(uriString);
            
            // foodstreetguide://poi/123?name=ABC
            if (uri.Scheme == "foodstreetguide" && uri.Host == "poi")
            {
                var poiId = ExtractPoiId(uri);
                var parameters = ParseQueryParameters(uri);
                
                Debug.WriteLine($"Navigating to POI: {poiId}");
                
                // Gửi message để MainPage xử lý
                WeakReferenceMessenger.Default.Send(new DeepLinkMessage
                {
                    Type = DeepLinkType.POI,
                    PoiId = poiId,
                    Parameters = parameters
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Deep link error: {ex.Message}");
        }
    }
    
    private static int ExtractPoiId(Uri uri)
    {
        // /poi/123 → 123
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && int.TryParse(segments[1], out int id))
        {
            return id;
        }
        return 0;
    }
    
    private static Dictionary<string, string> ParseQueryParameters(Uri uri)
    {
        var result = new Dictionary<string, string>();
        var query = HttpUtility.ParseQueryString(uri.Query);
        
        foreach (var key in query.AllKeys)
        {
            if (key != null)
                result[key] = query[key] ?? "";
        }
        
        return result;
    }
}

public enum DeepLinkType
{
    POI,
    Search,
    Settings
}

public class DeepLinkMessage
{
    public DeepLinkType Type { get; set; }
    public int PoiId { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}
```

### 2.3 Cập nhật `MainPage.xaml.cs` - Nhận message và hiển thị POI

```csharp
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace FoodStreetGuide;

public partial class MainPage : ContentPage
{
    // ... các services như cũ ...
    
    public MainPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        // ... khởi tạo services ...
        
        // Đăng ký nhận deep link message
        WeakReferenceMessenger.Default.Register<DeepLinkMessage>(this, async (recipient, message) =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (message.Type == DeepLinkType.POI && message.PoiId > 0)
                {
                    await NavigateToPOI(message.PoiId);
                }
            });
        });
    }
    
    private async Task NavigateToPOI(int poiId)
    {
        try
        {
            // Lấy POI từ database
            var poi = await _databaseService.GetPOIByIdAsync(poiId);
            
            if (poi == null)
            {
                await DisplayAlert("Thông báo", "Không tìm thấy quán ăn này", "OK");
                return;
            }
            
            // Di chuyển map đến vị trí POI
            var location = new Location(poi.Latitude, poi.Longitude);
            await map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(150)));
            
            // Hiển thị POI card
            ShowPOICard(poi);
            
            // Highlight pin trên map
            HighlightPOIPin(poi.Id);
            
            // Auto narration nếu bật
            if (_settingsService?.AutoNarrationEnabled == true && _narrationEngine != null)
            {
                await _narrationEngine.PlayNarrationAsync(poi);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"NavigateToPOI error: {ex.Message}");
        }
    }
    
    private void HighlightPOIPin(int poiId)
    {
        // Reset tất cả pin về màu mặc định
        foreach (var pin in _poiPins)
        {
            pin.MarkerId = null; // Bỏ highlight
        }
        
        // Highlight pin của POI được chọn
        if (_poiPinDictionary.TryGetValue(poiId, out Pin? selectedPin) && selectedPin != null)
        {
            // Có thể đổi icon hoặc màu để highlight
            selectedPin.MarkerId = "selected";
        }
    }
    
    private void ShowPOICard(POI poi)
    {
        // Cập nhật UI để hiển thị thông tin POI
        // (Tùy thuộc vào cách bạn thiết kế UI)
    }
}
```

---

## 🌐 Bước 3: Tạo QR Code trong Web Admin (PHP)

### 3.1 Cài đặt thư viện

```bash
cd FoodStreetGuide.Admin
composer require endroid/qr-code
```

### 3.2 File `generate-qr.php`

```php
<?php
require_once 'vendor/autoload.php';

use Endroid\QrCode\QrCode;
use Endroid\QrCode\Color\Color;
use Endroid\QrCode\Writer\PngWriter;
use Endroid\QrCode\Logo\Logo;

header('Content-Type: application/json');

try {
    $poiId = $_GET['poi_id'] ?? 0;
    $poiName = $_GET['name'] ?? 'Quán ăn';
    
    if ($poiId <= 0) {
        throw new Exception('Thiếu POI ID');
    }
    
    // 🔥 URL Scheme để mở app
    $qrContent = "foodstreetguide://poi/{$poiId}?name=" . urlencode($poiName);
    
    // Tạo QR Code
    $qrCode = QrCode::create($qrContent)
        ->setSize(400)
        ->setMargin(20)
        ->setForegroundColor(new Color(230, 81, 0))  // Màu cam #FF6B35
        ->setBackgroundColor(new Color(255, 255, 255)); // Nền trắng
    
    $writer = new PngWriter();
    
    // Thêm logo (tùy chọn)
    $logoPath = 'assets/logo.png'; // Đường dẫn logo
    if (file_exists($logoPath)) {
        $logo = Logo::create($logoPath)
            ->setResizeToWidth(80);
        $result = $writer->write($qrCode, $logo);
    } else {
        $result = $writer->write($qrCode);
    }
    
    // Lưu file
    $outputDir = 'uploads/qrcodes/';
    if (!is_dir($outputDir)) {
        mkdir($outputDir, 0777, true);
    }
    
    $filename = "qr_poi_{$poiId}.png";
    $filepath = $outputDir . $filename;
    $result->saveToFile($filepath);
    
    echo json_encode([
        'success' => true,
        'qr_url' => $filepath,
        'download_url' => "download.php?file=" . urlencode($filepath),
        'poi_id' => $poiId,
        'scan_url' => $qrContent,
        'message' => 'Tạo QR thành công'
    ]);
    
} catch (Exception $e) {
    echo json_encode([
        'success' => false,
        'error' => $e->getMessage()
    ]);
}
?>
```

### 3.3 Nút "Tạo QR" trong trang quản lý POI

```php
<!-- pois.php -->
<td>
    <button onclick="generateQR(<?php echo $poi['id']; ?>, '<?php echo htmlspecialchars($poi['name_vi']); ?>')" 
            class="btn btn-orange">
        📱 Tạo QR
    </button>
    
    <div id="qr-<?php echo $poi['id']; ?>" style="display:none; margin-top:10px;">
        <img src="" alt="QR Code" style="width:200px; height:200px;">
        <br>
        <a href="#" download class="btn btn-sm">Tải về</a>
    </div>
</td>

<script>
function generateQR(poiId, poiName) {
    fetch(`generate-qr.php?poi_id=${poiId}&name=${encodeURIComponent(poiName)}`)
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                const container = document.getElementById(`qr-${poiId}`);
                container.style.display = 'block';
                container.querySelector('img').src = data.qr_url;
                container.querySelector('a').href = data.download_url;
                
                alert('QR Code đã tạo! Quét để mở app.');
            } else {
                alert('Lỗi: ' + data.error);
            }
        });
}
</script>
```

---

## 🧪 Bước 4: Test QR Code

### 4.1 Test trên Android

```bash
# Dùng adb để test deep link
cd C:\Users\PC\FoodStreetGuide
adb shell am start -W -a android.intent.action.VIEW -d "foodstreetguide://poi/1"
```

### 4.2 Test trên iOS (Simulator)

```bash
# Mở Simulator rồi chạy:
xcrun simctl openurl booted "foodstreetguide://poi/1"
```

### 4.3 Test bằng QR thật

1. Tạo QR chứa: `foodstreetguide://poi/1`
2. Dùng camera iPhone/Android quét
3. Hiện popup "Mở bằng FoodStreetGuide" → Bấm OK
4. App mở và hiển thị POI #1

---

## 🎨 Mẫu QR Code đẹp

### Kích thước đề xuất:
| Vị trí | Kích thước | Ghi chú |
|--------|------------|---------|
| Bàn ăn | 5x5 cm | Dễ quét từ trên bàn |
| Cửa quán | 10x10 cm | Quét từ xa |
| Menu | 3x3 cm | Quét gần |

### Thiết kế QR:
```
┌─────────────────────────┐
│                         │
│    [QR CODE CAM]        │
│                         │
│   📱 QUÉT ĐỂ XEM        │
│   THÔNG TIN QUÁN        │
│                         │
│   🍜 Bánh Mì Huỳnh Hoa  │
│                         │
└─────────────────────────┘
```

---

## ✅ Checklist hoàn thành

- [ ] Cấu hình `AndroidManifest.xml` với intent-filter
- [ ] Cấu hình `Info.plist` với CFBundleURLTypes
- [ ] Tạo `DeepLinkHandler.cs` để parse URL
- [ ] Cập nhật `MainPage.xaml.cs` nhận message
- [ ] Cài đặt `endroid/qr-code` trong PHP
- [ ] Tạo API `generate-qr.php`
- [ ] Thêm nút "Tạo QR" trong admin
- [ ] Test trên Android
- [ ] Test trên iOS
- [ ] In QR dán thử tại quán

---

## 🐛 Xử lý lỗi thường gặp

### Lỗi 1: "Không có ứng dụng nào mở được"
**Nguyên nhân:** URL Scheme chưa đăng ký
**Fix:** Kiểm tra `AndroidManifest.xml` và `Info.plist`

### Lỗi 2: App mở nhưng không hiển thị POI
**Nguyên nhân:** DeepLinkHandler chưa gửi message
**Fix:** Kiểm tra `WeakReferenceMessenger.Default.Send()`

### Lỗi 3: QR không quét được
**Nguyên nhân:** QR quá nhỏ hoặc màu không tương phản
**Fix:** Dùng màu đen/cam trên nền trắng, kích thước tối thiểu 3x3cm

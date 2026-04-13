# 📱 Hướng dẫn tạo mã QR quét vào FoodStreetGuide App

## 🎯 Tổng quan

Mã QR cho phép người dùng **quét nhanh** để vào thẳng thông tin quán ăn trong app, không cần tìm kiếm thủ công.

---

## 📋 Định dạng mã QR

### Cấu trúc URL Scheme (Deep Link)

```
foodstreetguide://poi/{poi_id}
```

**Ví dụ:**
```
foodstreetguide://poi/123
foodstreetguide://poi/456?name=Bánh%20Mì%20Huỳnh%20Hoa
```

### Các tham số hỗ trợ:

| Tham số | Mô tả | Ví dụ |
|---------|-------|-------|
| `poi_id` | **(BẮT BUỘC)** ID của POI trong database | `123` |
| `name` | Tên quán (optional, để hiển thị preview) | `Bánh%20Mì%20Huỳnh%20Hoa` |
| `action` | Hành động sau khi mở (optional) | `show`, `navigate`, `checkin` |

---

## 🔧 Cách 1: Tạo mã QR đơn giản (Online Tool)

### Bước 1: Vào Web Admin lấy POI ID
1. Mở Web Admin: `http://localhost/FoodStreetGuide.Admin`
2. Vào menu **Quản lý POI**
3. Tìm quán cần tạo QR
4. Ghi lại **ID** (ví dụ: `123`)

### Bước 2: Tạo mã QR
Truy cập một trong các trang web sau:

| Website | URL |
|---------|-----|
| QR Code Generator | https://www.qr-code-generator.com/ |
| QRCode Monkey | https://www.qrcode-monkey.com/ |
| GoQR.me | https://goqr.me/ |

**Nhập nội dung:**
```
foodstreetguide://poi/123
```

**Tùy chọn:**
- Màu sắc: Cam (#FF6B35) để đồng bộ app
- Logo: Thêm logo FoodStreetGuide giữa QR

### Bước 3: Tải và in ấn
- Tải file PNG/SVG
- In ra giấy hoặc sticker dán tại quán
- Kích thước đề xuất: **3x3 cm** (tối thiểu), **5x5 cm** (lý tưởng)

---

## 💻 Cách 2: Tạo mã QR tự động trong Web Admin (PHP)

### Thêm vào file `pois.php`:

```php
<?php
// FoodStreetGuide.Admin/pois.php

require_once 'vendor/autoload.php';
use Endroid\QrCode\QrCode;
use Endroid\QrCode\Writer\PngWriter;

class POIQRGenerator {
    
    /**
     * Tạo mã QR cho POI
     * @param int $poiId ID của POI
     * @param string $poiName Tên POI
     * @return string Đường dẫn file QR đã tạo
     */
    public static function generateForPOI($poiId, $poiName) {
        // URL Scheme để mở app
        $qrContent = "foodstreetguide://poi/{$poiId}";
        
        // Hoặc dùng Universal Link (HTTPS) - khuyến nghị
        // $qrContent = "https://foodstreetguide.app/poi/{$poiId}";
        
        // Tạo QR Code
        $qrCode = QrCode::create($qrContent)
            ->setSize(300)
            ->setMargin(10)
            ->setForegroundColor(new \Endroid\QrCode\Color\Color(230, 81, 0)) // Cam
            ->setBackgroundColor(new \Endroid\QrCode\Color\Color(255, 255, 255));
        
        $writer = new PngWriter();
        $result = $writer->write($qrCode);
        
        // Lưu file
        $filename = "qr_poi_{$poiId}.png";
        $filepath = "uploads/qrcodes/{$filename}";
        
        // Tạo thư mục nếu chưa có
        if (!is_dir('uploads/qrcodes')) {
            mkdir('uploads/qrcodes', 0777, true);
        }
        
        $result->saveToFile($filepath);
        
        return $filepath;
    }
    
    /**
     * Tạo QR Code với logo ở giữa
     */
    public static function generateWithLogo($poiId, $poiName, $logoPath) {
        $qrCode = QrCode::create("foodstreetguide://poi/{$poiId}")
            ->setSize(400)
            ->setMargin(10);
        
        $writer = new PngWriter();
        
        // Thêm logo
        $logo = \Endroid\QrCode\Logo\Logo::create($logoPath)
            ->setResizeToWidth(50);
        
        $result = $writer->write($qrCode, $logo);
        
        $filepath = "uploads/qrcodes/qr_poi_{$poiId}_logo.png";
        $result->saveToFile($filepath);
        
        return $filepath;
    }
}

// API endpoint để tạo QR
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_GET['action']) && $_GET['action'] === 'generate-qr') {
    $poiId = $_POST['poi_id'] ?? 0;
    $poiName = $_POST['poi_name'] ?? '';
    
    try {
        $qrPath = POIQRGenerator::generateForPOI($poiId, $poiName);
        
        echo json_encode([
            'success' => true,
            'qr_url' => $qrPath,
            'download_url' => "download.php?file=" . urlencode($qrPath),
            'poi_id' => $poiId,
            'scan_content' => "foodstreetguide://poi/{$poiId}"
        ]);
    } catch (Exception $e) {
        echo json_encode([
            'success' => false,
            'error' => $e->getMessage()
        ]);
    }
    exit;
}
?>
```

### Thêm nút "Tạo QR" trong Admin UI:

```php
<!-- Trong danh sách POI -->
<td>
    <button onclick="generateQR(<?php echo $poi['id']; ?>, '<?php echo htmlspecialchars($poi['name']); ?>')" 
            class="btn btn-orange">
        📱 Tạo QR
    </button>
    <a href="uploads/qrcodes/qr_poi_<?php echo $poi['id']; ?>.png" 
       target="_blank" 
       class="btn btn-sm btn-secondary">
       Xem
    </a>
</td>

<script>
function generateQR(poiId, poiName) {
    fetch('pois.php?action=generate-qr', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `poi_id=${poiId}&poi_name=${encodeURIComponent(poiName)}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            alert('QR Code đã tạo thành công!');
            // Hiển thị QR
            showQRModal(data.qr_url, data.scan_content);
        } else {
            alert('Lỗi: ' + data.error);
        }
    });
}

function showQRModal(qrUrl, content) {
    // Hiển thị popup với QR code
    document.getElementById('qrImage').src = qrUrl;
    document.getElementById('qrContent').textContent = content;
    document.getElementById('qrModal').style.display = 'block';
}
</script>
```

---

## 📱 Cách 3: Xử lý QR trong App (.NET MAUI)

### Cập nhật `QRScanPage.xaml.cs`:

```csharp
using ZXing.Net.Maui;
using FoodStreetGuide.Models;
using FoodStreetGuide.Database;

namespace FoodStreetGuide;

public partial class QRScanPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    
    public QRScanPage()
    {
        InitializeComponent();
        _databaseService = new DatabaseService();
        
        // Khởi tạo camera scanner (cần thêm ZXing.Net.Maui)
        SetupScanner();
    }
    
    private void SetupScanner()
    {
        // Sử dụng ZXing để quét QR
        // Cài đặt: dotnet add package ZXing.Net.Maui
    }
    
    private async void ProcessQRCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập mã QR", "OK");
            return;
        }
        
        code = code.Trim();
        
        // Xử lý Deep Link: foodstreetguide://poi/{id}
        if (code.StartsWith("foodstreetguide://poi/"))
        {
            var poiId = ExtractPoiIdFromQR(code);
            await NavigateToPOI(poiId);
        }
        // Xử lý URL web: https://.../poi/{id}
        else if (code.Contains("/poi/"))
        {
            var poiId = ExtractPoiIdFromUrl(code);
            await NavigateToPOI(poiId);
        }
        // Nhập trực tiếp ID
        else if (int.TryParse(code, out int directId))
        {
            await NavigateToPOI(directId);
        }
        else
        {
            await DisplayAlert("Mã không hợp lệ", "Mã QR không đúng định dạng FoodStreetGuide", "OK");
        }
    }
    
    private int ExtractPoiIdFromQR(string qrCode)
    {
        // foodstreetguide://poi/123?name=ABC
        var uri = new Uri(qrCode);
        var path = uri.AbsolutePath; // /poi/123
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length >= 2 && segments[0] == "poi" && int.TryParse(segments[1], out int id))
        {
            return id;
        }
        
        return 0;
    }
    
    private int ExtractPoiIdFromUrl(string url)
    {
        // https://example.com/poi/123
        var match = System.Text.RegularExpressions.Regex.Match(url, @"/poi/(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
        {
            return id;
        }
        return 0;
    }
    
    private async Task NavigateToPOI(int poiId)
    {
        if (poiId <= 0)
        {
            await DisplayAlert("Lỗi", "Không tìm thấy mã POI", "OK");
            return;
        }
        
        // Lấy thông tin POI từ database
        var poi = await _databaseService.GetPOIByIdAsync(poiId);
        
        if (poi == null)
        {
            // Thử đồng bộ từ web
            await DisplayAlert("Thông báo", "Đang tải thông tin quán...", "OK");
            
            // TODO: Gọi API để lấy POI mới
            // poi = await _webAdminService.GetPOIFromWebAsync(poiId);
            
            if (poi == null)
            {
                await DisplayAlert("Không tìm thấy", "Quán ăn này chưa có trong hệ thống", "OK");
                return;
            }
        }
        
        // Đóng trang QR và về MainPage
        await Navigation.PopAsync();
        
        // Gửi message để MainPage focus vào POI
        WeakReferenceMessenger.Default.Send(new QRCodeScannedMessage(poi));
        
        // Hoặc dùng Event (nếu không dùng MVVM Toolkit)
        // MessagingCenter.Send(this, "QRCodeScanned", poi);
    }
}

// Message class cho WeakReferenceMessenger
public class QRCodeScannedMessage
{
    public POI POI { get; }
    
    public QRCodeScannedMessage(POI poi)
    {
        POI = poi;
    }
}
```

### Xử lý trong `MainPage.xaml.cs`:

```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    
    // Đăng ký nhận message từ QR scan
    WeakReferenceMessenger.Default.Register<QRCodeScannedMessage>(this, (recipient, message) =>
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await FocusOnPOI(message.POI);
        });
    });
}

private async Task FocusOnPOI(POI poi)
{
    // Tìm pin trên bản đồ
    if (_poiPinDictionary.TryGetValue(poi.Id, out Pin? pin))
    {
        // Di chuyển map đến POI
        var location = new Location(poi.Latitude, poi.Longitude);
        await map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromMeters(100)));
        
        // Hiển thị thông tin POI
        ShowPOICard(poi);
        
        // Nếu có narration, phát luôn
        if (_settingsService?.AutoNarrationEnabled == true)
        {
            await _narrationEngine?.PlayNarrationAsync(poi);
        }
    }
}
```

---

## 🔗 Cách 4: Universal Links (HTTPS) - Khuyến nghị

### Tạo file `.well-known/apple-app-site-association`:

```json
{
    "applinks": {
        "apps": [],
        "details": [
            {
                "appID": "TEAM_ID.com.companyname.foodstreetguide",
                "paths": ["/poi/*"]
            }
        ]
    }
}
```

### Tạo file `.well-known/assetlinks.json` (Android):

```json
[{
    "relation": ["delegate_permission/common.handle_all_urls"],
    "target": {
        "namespace": "android_app",
        "package_name": "com.companyname.foodstreetguide",
        "sha256_cert_fingerprints": ["YOUR_SHA256_FINGERPRINT"]
    }
}]
```

### Cấu hình trong `AndroidManifest.xml`:

```xml
<activity android:name="mainactivity" android:exported="true">
    <intent-filter android:autoVerify="true">
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="https" android:host="foodstreetguide.app" android:pathPrefix="/poi/" />
    </intent-filter>
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="foodstreetguide" android:host="poi" />
    </intent-filter>
</activity>
```

---

## 🎨 Mẫu QR Code đề xuất

### Thiết kế cho quán:
```
┌─────────────────────┐
│                     │
│   [QR CODE]         │
│                     │
│   QUÉT MÃ ĐỂ XEM    │
│   THÔNG TIN QUÁN    │
│                     │
│   🍜 Bánh Mì Huy    │
│   📍 26 Lê Thị Riêng│
│                     │
└─────────────────────┘
```

### Màu sắc:
- **QR màu cam:** `#FF6B35` (đồng bộ app)
- **Background:** Trắng hoặc trong suốt
- **Logo:** FoodStreetGuide ở giữa (tùy chọn)

---

## 📊 Checklist triển khai

- [ ] Cài đặt thư viện `endroid/qr-code` cho PHP
- [ ] Cài đặt `ZXing.Net.Maui` cho .NET MAUI
- [ ] Thêm nút "Tạo QR" trong Web Admin
- [ ] Cập nhật `QRScanPage.xaml.cs` để xử lý deep link
- [ ] Thêm URL Scheme vào `Info.plist` (iOS) và `AndroidManifest.xml`
- [ ] Test QR trên cả iOS và Android
- [ ] In QR dán tại các quán đối tác

---

## 💡 Tips

1. **Kích thước tối thiểu:** 3x3 cm để điện thoại quét được từ 10-15cm
2. **Độ tương phản:** QR màu đen/cam trên nền trắng tốt nhất
3. **Dự phòng:** Nên có text "Nhập mã thủ công" bên dưới QR
4. **Tracking:** Thêm query string `?ref=qr_table_01` để biết khách quét từ bàn nào

---

## 🌐 URL đầy đủ ví dụ:

```
foodstreetguide://poi/123?name=Bánh%20Mì%20Huỳnh%20Hoa&action=show&ref=qr_table_01
```

**Giải thích:**
- `poi/123` - POI ID = 123
- `name` - Tên hiển thị (URL encoded)
- `action=show` - Chỉ hiển thị thông tin (không tự động phát audio)
- `ref=qr_table_01` - Theo dõi nguồn (QR tại bàn 01)

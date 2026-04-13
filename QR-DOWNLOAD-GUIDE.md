# 📱 Hướng dẫn: Tạo QR Code tải App (APK Download)

## 🎯 Mục tiêu
Quét QR → Vào trang web → Tải APK → Cài đặt app

---

## 📁 Bước 1: Chuẩn bị file APK

### 1.1 Build APK từ .NET MAUI

```powershell
cd "C:\Users\PC\FoodStreetGuide"

# Build Release APK
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk

# Hoặc build AAB (Google Play)
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=aab
```

### 1.2 Tìm file APK sau khi build

**Vị trí file:**
```
C:\Users\PC\FoodStreetGuide\bin\Release\net8.0-android\com.companyname.foodstreetguide-Signed.apk
```

### 1.3 Copy APK vào thư mục Admin

```powershell
# Tạo thư mục nếu chưa có
mkdir "C:\Users\PC\FoodStreetGuide\FoodStreetGuide.Admin\downloads" -Force

# Copy APK
copy "bin\Release\net8.0-android\com.companyname.foodstreetguide-Signed.apk" "FoodStreetGuide.Admin\download.php"
cd "FoodStreetGuide.Admin"
rename "com.companyname.foodstreetguide-Signed.apk" "FoodStreetGuide.apk"
```

---

## 🌐 Bước 2: Upload lên Web Admin

### 2.1 Cấu trúc thư mục

```
FoodStreetGuide.Admin/
├── index.php          # Dashboard
├── pois.php           # Quản lý POI
├── download.php       # ⭐ Trang tải APK (đã tạo)
├── FoodStreetGuide.apk  # ⭐ File APK (build xong copy vào)
└── ...
```

### 2.2 URL trang tải

```
http://localhost/FoodStreetGuide.Admin/download.php

# Hoặc nếu deploy lên hosting:
https://your-domain.com/download.php
```

---

## 📱 Bước 3: Tạo QR Code

### 3.1 Cách 1: Online Tool (Nhanh nhất)

Truy cập:
- https://www.qr-code-generator.com/
- https://www.qrcode-monkey.com/
- https://goqr.me/

**Nhập URL:**
```
http://localhost/FoodStreetGuide.Admin/download.php
```

**Tùy chỉnh:**
- Màu: #FF6B35 (cam)
- Logo: Thêm icon 🍜 ở giữa
- Kích thước: 1000x1000px (để in)

### 3.2 Cách 2: Tạo bằng PHP (Tự động)

Tạo file `generate-download-qr.php`:

```php
<?php
require_once 'vendor/autoload.php';

use Endroid\QrCode\QrCode;
use Endroid\QrCode\Color\Color;
use Endroid\QrCode\Writer\PngWriter;
use Endroid\QrCode\Logo\Logo;

// URL đến trang download
$downloadUrl = "https://" . $_SERVER['HTTP_HOST'] . "/FoodStreetGuide.Admin/download.php";

// Tạo QR Code
$qrCode = QrCode::create($downloadUrl)
    ->setSize(400)
    ->setMargin(20)
    ->setForegroundColor(new Color(255, 107, 53))  // Cam #FF6B35
    ->setBackgroundColor(new Color(255, 255, 255)); // Trắng

$writer = new PngWriter();

// Thêm logo (tùy chọn)
$logoPath = 'assets/logo.png';
if (file_exists($logoPath)) {
    $logo = Logo::create($logoPath)->setResizeToWidth(80);
    $result = $writer->write($qrCode, $logo);
} else {
    $result = $writer->write($qrCode);
}

// Lưu file
$filename = 'qr-download-app.png';
$result->saveToFile($filename);

echo "QR Code đã tạo: $filename<br>";
echo "URL: $downloadUrl<br>";
echo "<img src='$filename' style='max-width:300px;'>";
?>
```

### 3.3 Cách 3: Tạo từ Web Admin Panel

Thêm vào `pois.php` (trang admin):

```php
<!-- Nút tạo QR -->
<div class="card mb-4">
    <div class="card-header">
        <h5><i class="bi bi-qr-code me-2"></i>QR Code Tải App</h5>
    </div>
    <div class="card-body text-center">
        <?php
        $downloadUrl = "https://" . $_SERVER['HTTP_HOST'] . "/FoodStreetGuide.Admin/download.php";
        $qrApiUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=" . urlencode($downloadUrl);
        ?>
        <img src="<?php echo $qrApiUrl; ?>" alt="QR Download" class="img-fluid mb-3" style="max-width:250px;">
        <p class="text-muted">Quét để tải app</p>
        <a href="<?php echo $qrApiUrl; ?>" download class="btn btn-primary">
            <i class="bi bi-download me-2"></i>Tải QR
        </a>
    </div>
</div>
```

---

## 🎨 Bước 4: Thiết kế Poster/Tờ rơi

### 4.1 Mẫu Poster đẹp

```
┌─────────────────────────────────────┐
│                                     │
│       🍜 FoodStreetGuide            │
│    Khám Phá Ẩm Thực Đường Phố      │
│                                     │
│    ┌───────────────────────┐       │
│    │                       │       │
│    │    [QR CODE LỚN]      │       │
│    │                       │       │
│    └───────────────────────┘       │
│                                     │
│      📱 QUÉT MÃ ĐỂ TẢI APP         │
│                                     │
│  1. Mở camera điện thoại           │
│  2. Quét mã QR bên trên            │
│  3. Tải và cài đặt app             │
│                                     │
│  ─────────────────────────────────  │
│  Hỗ trợ Android 8.0+                │
│  Phiên bản: 1.0.0                  │
│                                     │
└─────────────────────────────────────┘
```

### 4.2 Kích thước đề xuất:

| Mục đích | Kích thước QR | Ghi chú |
|----------|--------------|---------|
| Tờ rơi A5 | 4x4 cm | Quét từ 5-10cm |
| Poster A3 | 8x8 cm | Quét từ xa |
| Standee | 15x15 cm | Quét từ rất xa |
| Sticker nhỏ | 3x3 cm | Dán bàn |

---

## 📲 Bước 5: Test quy trình

### 5.1 Test trên Android

```powershell
# 1. Đảm bảo APK và download.php đã upload
# 2. Tạo QR code chứa URL
# 3. Dùng điện thoại Android quét
# 4. Kiểm tra:
#    ✅ Hiện trang download
#    ✅ Nút "Tải xuống APK" hoạt động
#    ✅ File APK tải về thành công
#    ✅ Cài đặt được
```

### 5.2 Test trên iOS (Expected failure)

```
Kết quả mong đợi: 
- QR quét được
- Hiện trang web
- Nhưng KHÔNG tải được APK (iOS không hỗ trợ)
→ Cần thông báo: "Vui lòng dùng điện thoại Android"
```

---

## 🔧 Xử lý lỗi thường gặp

### Lỗi 1: "Không tìm thấy file APK"

**Nguyên nhân:** Chưa copy APK vào thư mục admin

**Fix:**
```powershell
cd "C:\Users\PC\FoodStreetGuide"
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk
copy "bin\Release\net8.0-android\com.companyname.foodstreetguide-Signed.apk" "FoodStreetGuide.Admin\FoodStreetGuide.apk"
```

### Lỗi 2: "Không cài được APK" (Parse Error)

**Nguyên nhân:** APK không sign đúng hoặc min SDK quá cao

**Fix trong .csproj:**
```xml
<PropertyGroup Condition="'$TargetFramework' == 'net8.0-android'">
    <AndroidPackageFormat>apk</AndroidPackageFormat>
    <AndroidKeyStore>True</AndroidKeyStore>
    <AndroidSigningKeyStore>myapp.keystore</AndroidSigningKeyStore>
    <AndroidSigningKeyAlias>mykey</AndroidSigningKeyAlias>
    <AndroidSigningStorePass>password</AndroidSigningStorePass>
    <MinimumSupportedAndroidVersion>21</MinimumSupportedAndroidVersion>
</PropertyGroup>
```

### Lỗi 3: "Không quét được QR"

**Nguyên nhân:** QR quá nhỏ hoặc độ phân giải thấp

**Fix:**
- Kích thước tối thiểu: 3x3 cm
- Độ phân giải: 300 DPI
- Màu: Đen/Cam trên nền trắng (high contrast)

---

## 📊 Checklist hoàn thành

- [ ] Build APK thành công
- [ ] Copy APK vào `FoodStreetGuide.Admin/`
- [ ] Upload `download.php` lên server
- [ ] Test truy cập URL từ browser
- [ ] Tạo QR code
- [ ] Test quét QR bằng điện thoại Android
- [ ] Test tải APK
- [ ] Test cài đặt app
- [ ] In QR code dán thử

---

## 💡 Tips

1. **Giảm dung lượng APK:**
   - Bỏ ảnh không cần thiết
   - Dùng AOT compilation: `-p:RunAOTCompilation=true`
   - Strip debug symbols

2. **Tăng tốc download:**
   - Nén APK bằng 7-Zip (tùy chọn)
   - Dùng CDN hoặc hosting nhanh

3. **Theo dõi download:**
   - Thêm Google Analytics
   - Log số lượt tải trong database

4. **Cập nhật version:**
   - Đổi tên file: `FoodStreetGuide-v1.1.apk`
   - Cập nhật version trong `download.php`
   - Tạo QR mới

---

## 🌐 URL đầy đủ cho production

```
https://your-domain.com/FoodStreetGuide.Admin/download.php

Ví dụ:
- Local: http://localhost/FoodStreetGuide.Admin/download.php
- Hosting: https://foodstreetguide.com/download
- GitHub Pages: https://giahuyzawe.github.io/Do-an-C-sharp/download
```

---

## 🎉 Kết quả cuối cùng

User quét QR → Trình duyệt mở → Hiện trang đẹp → Tải APK → Cài app → Dùng! 🚀

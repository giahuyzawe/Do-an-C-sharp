# 🧪 Test QR Code & Deep Link KHÔNG cần điện thoại thật

## 🎯 Các cách test không cần device thật

---

## Cách 1: Android Emulator (Khuyến nghị)

### Bước 1.1: Cài đặt Android Emulator

**Có 2 lựa chọn:**

**A. Dùng Android Studio Emulator (Official)**
```powershell
# Tải Android Studio từ: https://developer.android.com/studio
# Sau đó cài đặt SDK và tạo Virtual Device

# Hoặc dùng SDK Manager có sẵn trong Visual Studio
```

**B. Dùng VS2022 Android Emulator**
```powershell
# Trong Visual Studio: Tools → Android → Android Device Manager
# Tạo new device: Pixel 5 API 33 (Android 13)
# RAM: 4GB, Storage: 4GB
```

### Bước 1.2: Deploy App lên Emulator

```powershell
cd "C:\Users\PC\FoodStreetGuide"

# Build và deploy
dotnet build -t:Run -f net8.0-android

# Hoặc từ VS: F5 (chọn Android Emulator)
```

### Bước 1.3: Test Deep Link qua ADB

```powershell
# Khi emulator đang chạy, mở PowerShell:

# Test cold start (app đóng hoàn toàn)
adb shell am start -a android.intent.action.VIEW -d "foodstreetguide://poi/1"

# Test warm start (app đang chạy background)
adb shell am start -W -a android.intent.action.VIEW -d "foodstreetguide://poi/2?name=Bánh%20Mì"

# Test với HTTPS URL (nếu có universal link)
adb shell am start -a android.intent.action.VIEW -d "https://foodstreetguide.app/poi/1"
```

### Bước 1.4: Test QR Code trong Emulator

**Cách A: Dùng camera máy tính**
```powershell
# 1. Hiển thị QR trên màn hình (mở browser hoặc image viewer)
# 2. Trong Android Emulator, mở Camera app
# 3. Chĩa camera emulator về phía màn hình laptop
# 4. Quét QR

# Hoặc dùng file QR trong gallery:
adb push qr_poi_1.png /sdcard/Pictures/
adb shell am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file:///sdcard/Pictures/qr_poi_1.png
```

**Cách B: Dùng Virtual Scene Camera**
```powershell
# Trong Android Emulator, mở Extended Controls (cái 3 chấm ⋮)
# → Camera → Virtual Scene
# Chọn image có QR code để test
```

---

## Cách 2: Test bằng Windows (Máy tính)

### 2.1 Dùng Protocol Handler trong Windows

**Tạo file test protocol:**
```powershell
# Tạo file .reg để đăng ký protocol handler (chỉ test)

@"
Windows Registry Editor Version 5.00

[HKEY_CLASSES_ROOT\foodstreetguide]
@="URL:FoodStreetGuide Protocol"
"URL Protocol"=""

[HKEY_CLASSES_ROOT\foodstreetguide\shell]

[HKEY_CLASSES_ROOT\foodstreetguide\shell\open]

[HKEY_CLASSES_ROOT\foodstreetguide\shell\open\command]
@="cmd /c echo Deep link received: %1 && pause"
"@ | Out-File -FilePath "test-protocol.reg" -Encoding ASCII

# Chạy file reg để đăng ký protocol
cmd /c "test-protocol.reg"
```

### 2.2 Test URL Scheme trong browser

```html
<!-- Tạo file test-deep-link.html -->
<!DOCTYPE html>
<html>
<head>
    <title>Test Deep Link</title>
</head>
<body style="font-family: Arial; padding: 50px;">
    <h1>🔗 Test FoodStreetGuide Deep Link</h1>
    
    <!-- Các link test -->
    <p><a href="foodstreetguide://poi/1" style="font-size: 20px;">
        📱 Test: foodstreetguide://poi/1
    </a></p>
    
    <p><a href="foodstreetguide://poi/2?name=Bánh%20Mì%20Huỳnh%20Hoa" style="font-size: 20px;">
        📱 Test: foodstreetguide://poi/2 (with name)
    </a></p>
    
    <hr>
    
    <h2>🧪 Nếu app chưa cài:</h2>
    <p>Hiện lỗi "Không tìm thấy ứng dụng" → Expected behavior</p>
    
    <h2>✅ Nếu app đã cài:</h2>
    <p>Hiện popup "Mở bằng FoodStreetGuide" → Success!</p>

</body>
</html>
```

**Mở file này bằng browser và bấm vào các link.**

### 2.3 Dùng QR Code Generator online

Truy cập các trang sau để tạo QR chứa deep link:

| Trang | URL | Cách dùng |
|-------|-----|-----------|
| QRCode Monkey | https://www.qrcode-monkey.com/ | Nhập `foodstreetguide://poi/1` → Tải QR |
| QR Code Generator | https://www.qr-code-generator.com/ | Chọn URL → Nhập deep link |
| GoQR.me | https://goqr.me/ | Nhập text → QR xuất hiện ngay |

**Test bằng camera laptop:**
1. Tạo QR trên trang web
2. Chụp màn hình QR
3. Dùng camera laptop để quét (nếu có)
4. Hoặc mở ảnh QR trên điện thoại khác để test

---

## Cách 3: Test bằng iOS Simulator (nếu có Mac)

```bash
# Trên MacBook, chạy lệnh:

# Mở Simulator
open -a Simulator

# Test deep link
xcrun simctl openurl booted "foodstreetguide://poi/1"

# Hoặc dùng Safari trong Simulator
# Mở Safari → Nhập foodstreetguide://poi/1
```

---

## Cách 4: BlueStacks / NoxPlayer (Android trên Windows)

### 4.1 Cài đặt BlueStacks

```powershell
# Tải từ: https://www.bluestacks.com/
# Cài đặt và đăng nhập Google Account
```

### 4.2 Cài app vào BlueStacks

```powershell
# Build app thành APK
cd "C:\Users\PC\FoodStreetGuide"
dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk

# APK sẽ ở: bin\Release\net8.0-android\com.companyname.foodstreetguide-Signed.apk

# Kéo thả APK vào BlueStacks để cài
```

### 4.3 Test trong BlueStacks

```powershell
# Dùng ADB kết nối BlueStacks (port 5555)
adb connect localhost:5555

# Test deep link
adb shell am start -a android.intent.action.VIEW -d "foodstreetguide://poi/1"

# Hoặc quét QR:
# 1. Mở camera trong BlueStacks
# 2. Hiển thị QR trên màn hình laptop
# 3. Quét bình thường
```

---

## Cách 5: Test bằng Unit Test / Mock

### 5.1 Tạo test project

```csharp
// FoodStreetGuide.Tests/DeepLinkTests.cs

using Xunit;
using FoodStreetGuide;

public class DeepLinkTests
{
    [Theory]
    [InlineData("foodstreetguide://poi/1", 1, "")]
    [InlineData("foodstreetguide://poi/123?name=Test", 123, "Test")]
    [InlineData("foodstreetguide://poi/456?name=Bánh%20Mì", 456, "Bánh Mì")]
    public void ParseDeepLink_ValidUrl_ReturnsPoiId(string url, int expectedId, string expectedName)
    {
        // Arrange
        var uri = new Uri(url);
        
        // Act
        var poiId = DeepLinkHandler.ExtractPoiId(uri);
        var parameters = DeepLinkHandler.ParseQueryParameters(uri);
        
        // Assert
        Assert.Equal(expectedId, poiId);
        if (!string.IsNullOrEmpty(expectedName))
        {
            Assert.Equal(expectedName, parameters["name"]);
        }
    }
    
    [Theory]
    [InlineData("https://example.com/poi/1", 1)]
    [InlineData("https://foodstreetguide.app/poi/99", 99)]
    public void ExtractFromUrl_ValidUrl_ReturnsPoiId(string url, int expectedId)
    {
        var id = DeepLinkHandler.ExtractPoiIdFromUrl(url);
        Assert.Equal(expectedId, id);
    }
}
```

### 5.2 Chạy test

```powershell
cd "C:\Users\PC\FoodStreetGuide"
dotnet test
```

---

## Cách 6: Dùng Browser DevTools (Test nhanh)

### 6.1 Tạo bookmarklet test

```javascript
// Tạo bookmark trong browser với URL:
javascript:(function(){window.location='foodstreetguide://poi/1';})();

// Tên: "Test FoodStreetGuide"
```

**Cách dùng:**
1. Tạo bookmark với code trên
2. Mở trang web bất kỳ
3. Bấm bookmark → Chuyển sang app (nếu cài)

---

## 📋 Summary: Khi nào dùng cách nào?

| Cách | Ưu điểm | Nhược điểm | Khi nào dùng |
|------|---------|------------|--------------|
| **Android Emulator** | Giống thật nhất | Chậm, tốn RAM | Test UI/UX đầy đủ |
| **ADB Test** | Nhanh, không cần UI | Không test được camera | Test logic deep link |
| **BlueStacks** | Dễ dùng, có camera | Cài đặt lâu | Test QR thực tế |
| **Browser Test** | Siêu nhanh | Không chắc chắn 100% | Test URL scheme nhanh |
| **Unit Test** | Tự động, nhanh | Không test UI | Test parse logic |

---

## ✅ Kết luận

**Không cần điện thoại thật**, bạn vẫn có thể:
- ✅ Test deep link bằng ADB + Emulator
- ✅ Test QR bằng BlueStacks
- ✅ Test parse logic bằng Unit Test
- ✅ Verify URL scheme bằng browser

**Khuyến nghị:**
1. **Developer test:** Dùng ADB + Android Emulator
2. **Integration test:** Dùng BlueStacks để quét QR thật
3. **CI/CD:** Dùng Unit Test cho parse logic

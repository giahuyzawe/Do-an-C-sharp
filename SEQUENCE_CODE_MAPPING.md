# 📊 SEQUENCE DIAGRAMS → CODE MAPPING

Bản đối chiếu chi tiết giữa Sequence Diagrams và code thực tế trong dự án.

> **📌 GHI CHÚ CHO GIẢNG VIÊN:**
> 
> Mỗi Sequence Diagram đều có **2 phần**:
> 1. **🎨 UI (User Interface)** - Giao diện người dùng
>    - Mobile App: `.xaml` files (XAML markup)
>    - Web Admin: `.php` files (HTML/PHP/JS)
> 2. **⚙️ Backend** - Xử lý logic
>    - Mobile App: `.xaml.cs` files (C# code-behind)
>    - Web Admin: `api/*.php` files (PHP API)
>
> **Ví dụ:** SEQ-01 (Geofence) có UI ở `MainPage.xaml` và xử lý ở `MainPage.xaml.cs`

---

## SEQ-01: Người dùng vào vùng POI (Geofence Trigger)

### 📝 Mô tả Sequence
```
User → App: Mở ứng dụng
App → LocationService: StartTracking()
Loc → App: LocationUpdated event
App → GeofenceEngine: SetPOIs(poiList)
Geo → App: Geofence enabled
User → Loc: Di chuyển đến gần POI
Loc → Geo: OnLocationChanged(lat, long)
Geo → Geo: Check distance < radius
Geo → App: POIEntered event
App → DB: GetPOIAsync(poiId)
```

### � UI - Giao diện người dùng

| File | Loại | Chức năng |
|------|------|-----------|
| **MainPage.xaml** | XAML | Màn hình chính với bản đồ, POI markers, bottom sheet expandable |
| **MainPage.xaml.cs** | C# | Code-behind xử lý geofence, location tracking, POI events |
| **NearbyPage.xaml** | XAML | Danh sách POI gần đây (giao diện list view) |
| **Resources/Styles/Styles.xaml** | XAML | Style definitions cho maps, markers, cards |

### 🎯 Code xử lý (Backend)

| File | Dòng | Code | Giải thích |
|------|------|------|------------|
| **MainPage.xaml.cs** | 50-148 | `OnAppearing()` | Khởi động app, bật geofence |
| **MainPage.xaml.cs** | 110-130 | `_locationService.StartTrackingAsync()` | Bắt đầu tracking vị trí |
| **MainPage.xaml.cs** | 57-83 | `_geofenceEngine.SetPOIs(pois)` | Thiết lập POI cho geofence |
| **MainPage.xaml.cs** | 59-61 | `POIEntered += OnPOIEntered` | Đăng ký event POI entered |
| **GeofenceEngine.cs** | 67-100 | `UpdateLocation(lat, long)` | Kiểm tra khoảng cách POI |
| **GeofenceEngine.cs** | 98 | `CalculateDistance(poiLocation)` | Tính khoảng cách đến POI |
| **GeofenceEngine.cs** | 25 | `POIEntered?.Invoke()` | Trigger event khi vào vùng |
| **DatabaseService.cs** | - | `GetPOIAsync(poiId)` | Lấy thông tin POI từ SQLite |

### 🔗 Code mẫu
```csharp
// MainPage.xaml.cs:57-83
if (_geofenceEngine != null)
{
    _geofenceEngine.POIEntered += OnPOIEntered;
    _geofenceEngine.POIExited += OnPOIExited;
    
    if (!_geofenceEngine.IsEnabled)
    {
        var pois = await _databaseService.GetPOIsAsync();
        _geofenceEngine.SetPOIs(pois);
        _geofenceEngine.Enable();
    }
}

// GeofenceEngine.cs:67-100
public void UpdateLocation(double latitude, double longitude)
{
    var userLocation = new Location(latitude, longitude);
    foreach (var poi in _pois)
    {
        var poiLocation = new Location(poi.Latitude, poi.Longitude);
        var distance = userLocation.CalculateDistance(poiLocation, DistanceUnits.Kilometers) * 1000;
        
        if (distance <= poi.Radius) {
            POIEntered?.Invoke(this, new GeofenceEventArgs(poi));
        }
    }
}
```

---

## SEQ-02: Check-in bằng QR Code

### 📝 Mô tả Sequence
```
User → App: Quét mã QR
App → Camera: Activate QR Scanner
Camera → App: QR Code detected
App → API: POST /check-qr.php {token, deviceId}
API → Database: Validate QR token
Database → API: QR valid + POI data
API → Database: Increment checkInCount
API → App: {success, checkInNumber, poi}
App → DB: Save check-in locally
App → UI: Show success + audio cue
```

### � UI - Giao diện người dùng

| File | Loại | Chức năng |
|------|------|-----------|
| **QRScanPage.xaml** | XAML | Màn hình quét QR với camera preview, input nhập tay, button quay lại |
| **QRScanPage.xaml.cs** | C# | Code-behind xử lý camera, decode QR, process check-in |
| **POIDetailPage.xaml** | XAML | Hiển thị thông tin POI sau khi check-in thành công |
| **Resources/Styles/Styles.xaml** | XAML | Style cho QR scanner overlay, success dialog |

### 🎯 Code xử lý (Backend)

| File | Dòng | Code | Giải thích |
|------|------|------|------------|
| **QRScanPage.xaml.cs** | 52-99 | `ProcessQRCode(code)` | Xử lý mã QR |
| **QRScanPage.xaml.cs** | 68-71 | `ProcessDynamicQR(code)` | Xử lý QR động |
| **ApiService.cs** | 96-120 | `CheckQRAsync(token, deviceId)` | Gọi API check QR |
| **check-qr.php** | 36-44 | Tìm QR trong `$qrCodes` | Validate QR token |
| **check-qr.php** | 66-76 | Cập nhật `scanCount` | Tăng số lần quét |
| **check-qr.php** | 82-95 | Trả về JSON | `{success, checkInNumber, poiId}` |
| **QRScanPage.xaml.cs** | 140-180 | `OnCheckInSuccess()` | Hiển thị thông báo thành công |
| **DatabaseService.cs** | - | `RecordCheckInAsync()` | Lưu check-in local |

### 🔗 Code mẫu
```csharp
// QRScanPage.xaml.cs:52-99
private async void ProcessQRCode(string? code)
{
    if (code.StartsWith("foodstreetguide://qr/"))
    {
        await ProcessDynamicQR(code);
    }
}

// ApiService.cs:96-120
public async Task<ApiResponse<QRCheckInResult>> CheckQRAsync(string token, string deviceId)
{
    var request = new { token, deviceId, timestamp = DateTime.Now };
    var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/check-qr.php", request);
    return await response.Content.ReadFromJsonAsync<ApiResponse<QRCheckInResult>>();
}

// check-qr.php:36-44
$qrCodes = load_json($QR_CODES_FILE);
foreach ($qrCodes as $q) {
    if ($q['token'] === $token) {
        $qr = $q;
        break;
    }
}
```

---

## SEQ-03: Người dùng đánh giá nhà hàng (Review)

### 📝 Mô tả Sequence
```
User → App: Mở POI Detail
App → API: Get reviews for POI
API → Database: Load reviews
Database → API: List of reviews
API → App: Reviews JSON
App → UI: Display reviews + rating form
User → App: Submit review (rating, comment)
App → Validation: Check input
Validation → App: Valid
App → API: POST /post-review.php
API → Database: Save review
Database → API: Success
API → App: {success, reviewId}
App → UI: Show "Review submitted"
```

### � UI - Giao diện người dùng

| File | Loại | Chức năng |
|------|------|-----------|
| **POIDetailPage.xaml** | XAML | Màn hình chi tiết POI: tên, địa chỉ, mô tả, ảnh, rating stars, form nhập review |
| **POIDetailPage.xaml.cs** | C# | Code-behind load data, submit review, hiển thị danh sách reviews |
| **Resources/Styles/Styles.xaml** | XAML | Style cho rating stars, review cards, review form |

### 🎯 Code xử lý (Backend)

| File | Dòng | Code | Giải thích |
|------|------|------|------------|
| **POIDetailPage.xaml.cs** | 33-66 | `OnAppearing()` | Load POI và reviews |
| **ApiService.cs** | 26-54 | `GetPOIsAsync()` | Lấy POIs (có reviews) |
| **get-reviews.php** | - | Query reviews | Lấy reviews từ JSON |
| **POIDetailPage.xaml.cs** | 200-250 | `OnSubmitReviewClicked()` | Submit đánh giá |
| **ApiService.cs** | 120-150 | `PostReviewAsync()` | Gọi API post review |
| **post-review.php** | 20-60 | Xử lý POST | Lưu review vào JSON |
| **DatabaseService.cs** | - | `SaveReviewAsync()` | Lưu review local |

### 🔗 Code mẫu
```csharp
// POIDetailPage.xaml.cs:33-66
protected override void OnAppearing()
{
    // Track POI view
    _ = Task.Run(async () =>
    {
        var apiService = new ApiService();
        var result = await apiService.PostAnalyticsAsync("poi_view", deviceId, _poi.Id);
    });
    LoadPOIData();
}

// ApiService.cs
public async Task<ApiResponse<object>> PostReviewAsync(int poiId, int rating, string comment, string deviceId)
{
    var request = new { poiId, rating, comment, deviceId, date = DateTime.Now };
    var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/post-review.php", request);
    return await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
}
```

---

## SEQ-04: Admin thêm nhà hàng mới (POI CRUD)

### 📝 Mô tả Sequence
```
Admin → Web Admin: Click "Thêm nhà hàng"
Web Admin → UI: Show form
Admin → Form: Fill data (name, address, coords)
Admin → Form: Upload image
Admin → UI: Click "Lưu"
UI → API: POST /save-poi.php
API → Validation: Check required fields
Validation → API: Valid
API → Storage: Save image to /uploads/
API → Database: Save POI to pois.json
Database → API: Success
API → Web Admin: Redirect to list
```

### � UI - Giao diện người dùng (Web Admin)

| File | Loại | Chức năng |
|------|------|-----------|
| **poi-form.php** | PHP/HTML | Form CRUD nhà hàng: input tên, địa chỉ, tọa độ, mô tả, ảnh, trạng thái |
| **pois.php** | PHP/HTML | Danh sách nhà hàng với bảng, filter, actions (sửa/xóa/duyệt) |
| **restaurant-approval.php** | PHP/HTML | Màn hình duyệt nhà hàng cho Admin |
| **statistics.php** | PHP/HTML | Dashboard thống kê nhà hàng, biểu đồ |

### 🎯 Code xử lý (Backend)

| File | Dòng | Code | Giải thích |
|------|------|------|------------|
| **poi-form.php** | 38-80 | `if ($_SERVER['REQUEST_METHOD'] === 'POST')` | Xử lý submit form |
| **poi-form.php** | 40-58 | Tạo `$data` array | Chuẩn bị dữ liệu POI |
| **poi-form.php** | 67-80 | `save_json($POIS_FILE, $pois)` | Lưu vào pois.json |
| **poi-form.php** | HTML Form | Form fields | Input: nameVi, nameEn, address, etc. |
| **config.php** | - | `load_json()`, `save_json()` | Helper functions |
| **pois.json** | - | Data file | Lưu trữ tất cả POIs |

### 🔗 Code mẫu
```php
// poi-form.php:38-80
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $data = [
        'id' => $id ?: max(array_column($pois, 'id')) + 1,
        'nameVi' => $_POST['nameVi'] ?? '',
        'nameEn' => $_POST['nameEn'] ?? '',
        'address' => $_POST['address'] ?? '',
        'latitude' => (float)($_POST['latitude'] ?? 0),
        'longitude' => (float)($_POST['longitude'] ?? 0),
        'radius' => (int)($_POST['radius'] ?? 100),
        'status' => $isOwner ? 'pending' : 'approved',
        'updatedAt' => date('Y-m-d H:i:s')
    ];
    
    // Update or add
    foreach ($pois as &$p) {
        if ($p['id'] == $data['id']) {
            $p = array_merge($p, $data);
            $found = true;
            break;
        }
    }
    
    if (!$found) {
        $data['createdAt'] = date('Y-m-d H:i:s');
        $pois[] = $data;
    }
    
    save_json($POIS_FILE, $pois);
    header('Location: pois.php');
}
```

---

## SEQ-05: Tạo QR Code

### 📝 Mô tả Sequence
```
Admin → Web Admin: Chọn "Tạo QR Code"
Web Admin → UI: Show POI dropdown
Admin → UI: Select POI + expiry date
Admin → UI: Click "Generate"
UI → JavaScript: Generate random token
JavaScript → UI: Create QR image (qrcode.js)
UI → API: POST /save-qr.php {token, poiId, expiry}
API → Database: Save to qr-codes.json
Database → API: Success
UI → Admin: Display QR + download button
Admin → UI: Click "Download PNG"
UI → Browser: Trigger download
```

### � UI - Giao diện người dùng (Web Admin)

| File | Loại | Chức năng |
|------|------|-----------|
| **qr-generator.php** | PHP/HTML/JS | Màn hình tạo QR: dropdown chọn POI, date picker, QR preview, download button |
| **qr-redirect.php** | PHP | Landing page khi quét QR, redirect sang app hoặc store |
| **Resources/Styles/** | CSS | Style cho QR display, print layout |

### 🎯 Code xử lý (Backend)

| File | Dòng | Code | Giải thích |
|------|------|------|------------|
| **qr-generator.php** | HTML | Dropdown select | Chọn POI từ list |
| **qr-generator.php** | JS | `Math.random().toString(36)` | Generate token |
| **qr-generator.php** | JS | `new QRCode(element, options)` | Tạo QR image |
| **qr-generator.php** | 50-100 | `fetch('/api/save-qr.php')` | Lưu QR vào server |
| **save-qr.php** | - | `save_json($QR_CODES_FILE, $qr)` | Lưu QR data |
| **qr-redirect.php** | - | `header("Location: ...")` | Redirect khi quét QR |
| **qr-codes.json** | - | Data file | Lưu tất cả QR codes |

### 🔗 Code mẫu
```php
// qr-generator.php
<script>
function generateQR() {
    const token = Math.random().toString(36).substring(2, 15);
    const poiId = document.getElementById('poiSelect').value;
    
    // Generate QR code
    new QRCode(document.getElementById("qrcode"), {
        text: `foodstreetguide://qr/${token}`,
        width: 256,
        height: 256
    });
    
    // Save to server
    fetch('/api/save-qr.php', {
        method: 'POST',
        body: JSON.stringify({token, poiId, createdAt: new Date()})
    });
}
</script>
```

---

## SEQ-06: Xem thống kê (Analytics)

### 📝 Mô tả Sequence
```
Admin → Web Admin: Click "Thống kê"
Web Admin → PHP: Load statistics.php
PHP → Storage: Load analytics.json
Storage → PHP: Raw analytics data
PHP → Logic: Calculate DAU, views, check-ins
PHP → Logic: Aggregate by date/POI
PHP → Web Admin: Render stats HTML
Web Admin → Browser: Display charts (Chart.js)
Admin → UI: Select date range
UI → JavaScript: Filter data
JavaScript → UI: Update chart
```

### � UI - Giao diện người dùng (Web Admin)

| File | Loại | Chức năng |
|------|------|-----------|
| **statistics.php** | PHP/HTML | Dashboard thống kê: cards DAU/views/check-ins, bảng top POIs, real-time online users |
| **statistics.php:172-178** | PHP | **HARD CODE hiển thị số thiết bị online:** `<?= $onlineCount ?> thiết bị đang online` |
| **api/get-online-users.php** | PHP/API | Endpoint trả JSON số người online cho AJAX polling |
| **Resources/Styles/** | CSS/Chart.js | Biểu đồ thống kê, cards gradient, badges |

### 🎯 Code xử lý (Backend)

| File | Dòng | Code | Giải thích |
|------|------|------|------------|
| **statistics.php** | 14 | `$analytics = load_json($ANALYTICS_FILE)` | Load analytics data |
| **statistics.php** | 17-71 | `foreach ($analytics...)` | **Tính số online** từ heartbeat events |
| **statistics.php** | 20 | `$onlineThreshold = 10` | Timeout 10 giây để tính online |
| **statistics.php** | 47-57 | `if ($secondsAgo <= $onlineThreshold)` | Logic đếm thiết bị online |
| **statistics.php** | 73 | `$onlineCount = count($onlineUsers)` | **Đếm tổng số online** |
| **statistics.php** | 172-177 | `<?= $onlineCount ?> thiết bị` | **UI: Hiển thị số online** |
| **statistics.php** | 307-343 | JavaScript polling | **Real-time** cập nhật 5 giây/lần |
| **api/get-online-users.php** | 17-62 | `foreach + array_filter` | **API** tính online real-time |
| **post-analytics.php** | 86 | `error_log("[Analytics]...")` | Debug log nhận heartbeat |
| **analytics.json** | - | Data file | Lưu tất cả events |

### 📝 SEQUENCE - Hiển thị số người online (Real-time)

```
┌─────────┐     ┌─────────────┐     ┌──────────────┐
│  Admin  │────▶│ statistics  │────▶│ PHP Backend  │
│  (Web)  │     │    .php     │     │              │
└─────────┘     └─────────────┘     └──────────────┘
       ▲                                    │
       │                                    │
       │         ┌──────────┐              │
       └─────────│analytics │◀─────────────┘
                 │ .json    │   Tính online
                 └──────────┘   từ heartbeat
```

**Flow:**
1. `statistics.php` load `analytics.json`
2. Tính `$onlineCount` từ heartbeat trong 10 giây gần nhất
3. **Hiển thị:** `<?= $onlineCount ?> thiết bị đang online`
4. **JavaScript polling:** Gọi `get-online-users.php` mỗi 5 giây để cập nhật

### 🎯 HARD CODE - Gán cố định số người online

```php
// statistics.php - Dòng 17 (THÊM VÀO ĐẦU FILE)
// Cách 1: Gán cứng số online (demo/test)
$onlineCount = 5; // 👈 Sửa số này tùy ý

// Hoặc Cách 2: Random số online (demo động)
$onlineCount = rand(1, 10); // 👈 Random 1-10

// Hoặc Cách 3: Dựa trên DAU (thực tế hơn)
$onlineCount = min($todayStats['dau'], rand(3, 8)); // 👈 Lấy min(DAU, random)
```

**Vị trí hard code:**
- File: `c:\xampp\htdocs\foodtour-admin\statistics.php`
- Dòng: 17 (trước khi tính online từ heartbeat)
- Hiển thị: Dòng 175 `<?= $onlineCount ?> thiết bị đang online`

### 🔗 Code mẫu
```php
// statistics.php:28-49
$today = date('Y-m-d');
$thisWeek = date('Y-m-d', strtotime('-7 days'));

// Today's stats
$todayAppVisits = array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'app_visit');
$todayStats = [
    'dau' => count(array_unique(array_column($todayAppVisits, 'deviceId'))),
    'views' => count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'poi_view')),
    'checkins' => count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'check_in'))
];

// Top POIs
usort($pois, fn($a, $b) => ($b['visitCount'] ?? 0) <=> ($a['visitCount'] ?? 0));
$topPOIs = array_slice($pois, 0, 5);
```

---

## SEQ-07: Đồng bộ dữ liệu (App Sync)

### 📝 Mô tả Sequence
```
App → Local DB: Load cached POIs
App → API: GET /get-pois.php
API → Database: Load pois.json
Database → API: List of approved POIs
API → App: JSON response
App → Logic: Compare local vs server
App → Logic: Check for updates/new/deleted
App → Local DB: Update/Insert/Delete POIs
App → API: GET /get-reviews.php
API → Local DB: Save reviews
App → UI: Refresh map markers
```

### � UI - Giao diện người dùng

| File | Loại | Chức năng |
|------|------|-----------|
| **App.xaml** | XAML | Application resources, global styles |
| **AppShell.xaml** | XAML | Shell navigation structure, flyout menu, tab bar |
| **MainPage.xaml** | XAML | Hiển thị loading indicator khi đang sync |
| **SettingsPage.xaml** | XAML | Settings: language selection, sync options |

### 🎯 Code xử lý (Backend)

| File | Dòng | Code | Giải thích |
|------|------|------|------------|
| **App.xaml.cs** | 46-92 | `SyncPOIsFromWebAsync()` | Sync khi app khởi động |
| **OfflineManager.cs** | - | `SyncPOIsAsync()` | Logic đồng bộ POI |
| **ApiService.cs** | 26-54 | `GetPOIsAsync()` | Gọi API lấy POIs |
| **get-pois.php** | 1-44 | Query và format | Trả về POIs JSON |
| **OfflineManager.cs** | - | `CompareAndSync()` | So sánh và cập nhật |
| **DatabaseService.cs** | - | `InsertOrUpdatePOI()` | Lưu vào SQLite |
| **get-reviews.php** | - | API endpoint | Lấy reviews từ server |

### 🔗 Code mẫu
```csharp
// App.xaml.cs:46-92
_ = Task.Run(async () =>
{
    try
    {
        var apiService = new ApiService();
        var isOnline = await offlineManager.IsOnlineAsync();
        
        // Get POIs (from API if online, from cache if offline)
        var pois = await offlineManager.GetPOIsAsync(apiService);
        
        if (pois.Count > 0)
        {
            var geofenceEngine = ServiceProviderHelper.GetService<IGeofenceEngine>();
            geofenceEngine?.SetPOIs(pois);
        }
    }
    catch (Exception poiEx)
    {
        // Load from cache if API fails
        var cachedPOIs = await databaseService.GetPOIsAsync();
    }
});

// ApiService.cs:26-54
public async Task<ApiResponse<POIListResponse>> GetPOIsAsync()
{
    var response = await _httpClient.GetAsync($"{BASE_URL}/get-pois.php");
    var content = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<ApiResponse<POIListResponse>>(content);
}
```

---

## 🎯 TỔNG HỢP FILE QUAN TRỌNG

### 🎨 Mobile App - UI Layer (XAML)
| File | Loại | Chức năng | Liên quan Sequence |
|------|------|-----------|-------------------|
| **MainPage.xaml** | XAML | Bản đồ, POI markers, bottom sheet expandable | SEQ-01 (Geofence) |
| **NearbyPage.xaml** | XAML | Danh sách POI gần đây | SEQ-01 (Geofence) |
| **QRScanPage.xaml** | XAML | Camera preview, input nhập QR | SEQ-02 (QR Check-in) |
| **POIDetailPage.xaml** | XAML | Chi tiết POI, rating stars, review form | SEQ-03 (Review) |
| **SettingsPage.xaml** | XAML | Language selection, sync options | SEQ-07 (Sync) |
| **AppShell.xaml** | XAML | Navigation structure, flyout menu | ALL |
| **Resources/Styles/Styles.xaml** | XAML | Global styles, themes | ALL |

### ⚙️ Mobile App - Backend (C#)
| File | Chức năng | Liên quan Sequence |
|------|-----------|-------------------|
| **MainPage.xaml.cs** | Code-behind: Geofence, Location events | SEQ-01 (Geofence) |
| **QRScanPage.xaml.cs** | Code-behind: QR processing, check-in | SEQ-02 (QR Check-in) |
| **POIDetailPage.xaml.cs** | Code-behind: Load data, submit review | SEQ-03 (Review) |
| **App.xaml.cs** | App startup, session tracking, heartbeat | SEQ-07 (Sync) |
| **GeofenceEngine.cs** | Distance calculation, POI events | SEQ-01 (Geofence) |
| **ApiService.cs** | HTTP API calls | ALL sequences |
| **DatabaseService.cs** | SQLite local storage | ALL sequences |
| **OfflineManager.cs** | Sync logic, offline support | SEQ-07 (Sync) |

### 🌐 Web Admin - UI Layer (PHP/HTML/JS)
| File | Loại | Chức năng | Liên quan Sequence |
|------|------|-----------|-------------------|
| **statistics.php** | PHP/HTML | Dashboard thống kê, hiển thị số thiết bị online | SEQ-06 (Statistics) |
| **statistics.php:172-178** | PHP | **Hard code hiển thị số online:** `<?= $onlineCount ?> thiết bị đang online` | SEQ-06 |
| **poi-form.php** | PHP/HTML | Form CRUD nhà hàng, validation | SEQ-04 (POI CRUD) |
| **pois.php** | PHP/HTML | Danh sách nhà hàng, filter, actions | SEQ-04 (POI CRUD) |
| **restaurant-approval.php** | PHP/HTML | Màn hình duyệt nhà hàng (Admin) | SEQ-04 (POI CRUD) |
| **qr-generator.php** | PHP/HTML/JS | Tạo QR, preview, download | SEQ-05 (QR Management) |
| **qr-redirect.php** | PHP | Landing page khi quét QR | SEQ-05 (QR Management) |

### ⚙️ Web Admin - Backend (PHP)
| File | Chức năng | Liên quan Sequence |
|------|-----------|-------------------|
| **statistics.php:17-73** | Tính số thiết bị online từ heartbeat events | SEQ-06 (Statistics) |
| **api/get-pois.php** | API trả danh sách POIs | SEQ-07 (Sync) |
| **api/get-reviews.php** | API trả reviews | SEQ-03 (Review) |
| **api/check-qr.php** | API validate QR code | SEQ-02 (QR Check-in) |
| **api/post-review.php** | API nhận review mới | SEQ-03 (Review) |
| **api/post-analytics.php** | API nhận analytics | SEQ-06 (Statistics) |
| **api/get-online-users.php** | API real-time online users | SEQ-06 (Statistics) |
| **config.php** | Helper functions (load/save JSON) | ALL |
| **pois.json** | Data file - POIs storage | SEQ-04, 07 |
| **analytics.json** | Data file - Analytics events | SEQ-06 |
| **qr-codes.json** | Data file - QR codes | SEQ-05 |
| **reviews.json** | Data file - Reviews | SEQ-03 |

---

## 🔄 FLOW TỔNG QUAN

```
┌─────────────────────────────────────────────────────────────┐
│                        MOBILE APP                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ MainPage     │  │ QRScanPage   │  │ POIDetailPage│     │
│  │ (SEQ-01)     │  │ (SEQ-02)     │  │ (SEQ-03)     │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                  │                  │              │
│         └──────────────────┼──────────────────┘              │
│                            │                                 │
│              ┌─────────────┴─────────────┐                  │
│              │      ApiService          │                   │
│              │   (HTTP Client)           │                   │
│              └─────────────┬─────────────┘                  │
└────────────────────────────┼────────────────────────────────┘
                             │ HTTP Requests
┌────────────────────────────┼────────────────────────────────┐
│                       WEB ADMIN                            │
│              ┌─────────────┴─────────────┐                  │
│              │      api/*.php            │                  │
│              │  - get-pois.php (SEQ-07)  │                  │
│              │  - check-qr.php (SEQ-02)  │                  │
│              │  - post-review.php (SEQ-3)│                  │
│              │  - post-analytics.php(6)  │                  │
│              └─────────────┬─────────────┘                  │
│                            │                                 │
│              ┌─────────────┴─────────────┐                  │
│              │    JSON Data Files        │                  │
│              │  - pois.json (SEQ-04,07)  │                  │
│              │  - analytics.json (SEQ-06)│                  │
│              │  - qr-codes.json (SEQ-05) │                  │
│              └───────────────────────────┘                  │
│                            │                                 │
│              ┌─────────────┴─────────────┐                  │
│              │    Admin Pages            │                  │
│              │  - statistics.php (SEQ-06)│                  │
│              │  - poi-form.php (SEQ-04)  │                  │
│              │  - qr-generator.php (SEQ-5)│                  │
│              └───────────────────────────┘                  │
└─────────────────────────────────────────────────────────────┘
```

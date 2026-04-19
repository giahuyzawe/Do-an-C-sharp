# ✅ CHECKLIST TEST - FOOD STREET GUIDE

## 📋 Hướng dẫn Test
- [ ] Chạy Web Admin trên XAMPP
- [ ] Cài đặt app trên Android Emulator
- [ ] Đặt location emulator tại từng POI để test

---

## 🌐 PHẦN 1: WEB ADMIN TEST

### 1.1 Dashboard & POI Management
| # | Test Case | Cách Test | Pass? |
|---|-----------|-----------|-------|
| 1 | Truy cập Web Admin | Mở `http://localhost/foodtour-admin/` | |
| 2 | Đăng nhập | Dùng admin/admin | |
| 3 | Xem danh sách 20 POIs | Kiểm tra có đủ 20 nhà hàng | |
| 4 | Xem chi tiết POI | Click "Sửa" một nhà hàng | |
| 5 | Thêm POI mới | Click "Thêm nhà hàng" → Điền form → Lưu | |
| 6 | Cập nhật POI | Sửa tên/mô tả → Lưu | |
| 7 | Xóa POI | Click "Xóa" → Xác nhận | |

### 1.2 API Endpoints
| # | API | URL | Test |
|---|-----|-----|------|
| 1 | Get POIs | `http://localhost/foodtour-admin/api/get-pois.php` | Mở browser, check JSON |
| 2 | Post Analytics | `http://localhost/foodtour-admin/api/post-analytics.php` | Dùng Postman hoặc app |
| 3 | Check QR | `http://localhost/foodtour-admin/api/check-qr.php` | Test với app QR scan |

### 1.3 Statistics
| # | Test Case | Expected Result |
|---|-----------|-----------------|
| 1 | Mở `statistics.php` | Hiện dashboard thống kê |
| 2 | Kiểm tra DAU chart | Có dữ liệu hoặc "Chưa có dữ liệu" |
| 3 | Kiểm tra POI Views | Đếm đúng số lượt xem |
| 4 | Kiểm tra QR Scans | Đếm đúng số lượt check-in |

### 1.4 QR Management
| # | Test Case | Steps |
|---|-----------|-------|
| 1 | Tạo QR mới | Vào QR Generator → Chọn POI → Generate |
| 2 | Test QR scan | Dùng app scan QR trên màn hình |
| 3 | Kiểm tra số check-in | Vào Statistics xem count tăng |

---

## 📱 PHẦN 2: MOBILE APP TEST

### 2.1 Launch & Sync
| # | Test Case | Expected |
|---|-----------|----------|
| 1 | Mở app lần đầu | Splash screen → Loading "Đang đồng bộ..." → Map |
| 2 | Kiểm tra 20 pins | Trên map có 20 marker đỏ |
| 3 | Kiểm tra sync status | Logcat: "Loaded 20 POIs from database" |

### 2.2 Location & Geofence
| # | Test Case | Location | Expected |
|---|-----------|----------|----------|
| 1 | Set location Điểm 1 | `21.350000, 105.550000` | Hiện "Gần Phở Gà Vĩnh Phúc" |
| 2 | Set location Điểm 2 | `21.352000, 105.555000` | Hiện "Gần Bún Chả Vĩnh Yên" |
| 3 | Set location Điểm 3 | `21.348000, 105.553000` | Hiện "Gần Cà Phê Highlands" |
| 4 | Di chuyển xa tất cả POI | `21.300000, 105.500000` | Không hiện notification |
| 5 | Test geofence trigger | Di chuyển vào/vùng POI | Logcat: "POI Entered/Exited" |

### 2.3 Tab Navigation
| Tab | Test Case | Expected |
|-----|-----------|----------|
| **Bản đồ** | Hiện 20 pins | ✅ Tất cả POI hiển thị |
| **Bản đồ** | Click marker | POI card hiện thông tin |
| **Bản đồ** | Click "Chi tiết" | Mở POIDetailPage |
| **Khám phá** | 4 cards hiện tên thật | "Phở Gà Vĩnh Phúc",... |
| **Khám phá** | Filter "Gần nhất" | Sắp xếp theo khoảng cách GPS |
| **Khám phá** | Filter "Đánh giá cao" | Sắp xếp theo rating |
| **Khám phá** | Filter "Đang mở" | Nhà hàng mở lên đầu |
| **Đã lưu** | Lưu POI | ❤️ hiện trong danh sách |
| **Cài đặt** | Đổi ngôn ngữ | EN/VI chuyển đổi được |

### 2.4 POI Detail Page
| # | Feature | Test |
|---|---------|------|
| 1 | Hiển thị ảnh | Carousel hiện ảnh từ ImageUrl |
| 2 | Hiển thị mô tả | DescriptionVi đầy đủ |
| 3 | Hiển thị địa chỉ | Address hiển thị đúng |
| 4 | Hiển thị giờ mở | OpeningHours đúng |
| 5 | Click "Chỉ đường" | Mở Google Maps |
| 6 | Click ❤️ Lưu | POI vào tab "Đã lưu" |

### 2.5 Reviews Feature
| # | Test Case | Steps |
|---|-----------|-------|
| 1 | Xem đánh giá | Vào POI Detail → Kéo xuống "⭐ Đánh giá" |
| 2 | Thêm đánh giá | Click "+ Thêm đánh giá" → Chọn sao → Viết comment → Gửi |
| 3 | Kiểm tra hiển thị | Review mới hiện trong list |
| 4 | Kiểm tratrung bình | Average rating cập nhật |
| 5 | Kiểm tra database | SQLite có review record mới |

### 2.6 Offline Mode
| # | Scenario | Test |
|---|----------|------|
| 1 | Tắt WiFi | App vẫn hiện POIs (từ cache) |
| 2 | Sync khi online lại | POIs tự động refresh |
| 3 | Queue analytics | Offline → Online: analytics gửi lại |

### 2.7 QR Check-in
| # | Test Case | Expected |
|---|-----------|----------|
| 1 | Scan QR hợp lệ | "Check-in thành công!" |
| 2 | Scan QR lần 2 (trong 1h) | "Bạn đã check-in gần đây" |
| 3 | Scan QR hết hạn | "QR không hợp lệ" |
| 4 | Kiểm tra Web Admin | Check-in count tăng |

---

## 🔗 PHẦN 3: INTEGRATION TEST (Web ↔ App)

### 3.1 Sync Flow
```
[App] Mở app ──► [API] Get POIs ──► [Web Admin] Trả 20 POIs
    │                                              │
    ▼                                              ▼
[SQLite] Lưu cache ◄─── [App] Hiển thị ◄─── Parse JSON
    │
    ▼
[Geofence] Update POIs
```

| Step | Check | Result |
|------|-------|--------|
| 1 | App gọi API | Logcat: "GET /get-pois.php" |
| 2 | Web Admin nhận request | Apache access log |
| 3 | API trả JSON đúng | 20 items, đầy đủ fields |
| 4 | App parse & lưu SQLite | Database có 20 records |
| 5 | Geofence update | "Geofence updated with 20 POIs" |

### 3.2 Analytics Flow
```
[User] View POI ──► [App] Record local + Queue
    │
    ▼
[API] POST /post-analytics.php ──► [Web Admin] Lưu DB
    │
    ▼
[Statistics] DAU +1, POI Views +1
```

| Event | App Log | Web Admin |
|-------|---------|-----------|
| App mở | "Analytics sent: app_visit" | DAU tăng |
| Xem POI | "Analytics sent: poi_view" | POI Views tăng |
| Check-in QR | "Analytics sent: check_in" | Check-in count tăng |

### 3.3 Review Flow
```
[User] Gửi review ──► [SQLite] Lưu local
    │
    ▼
[Sync khi có API] (chưa có Web Admin API cho review)
    │
    ▼
[Chi hiển thị trên app]
```

---

## 📊 PHẦN 4: TEST RESULTS

### Summary
| Category | Total | Pass | Fail |
|----------|-------|------|------|
| Web Admin | 15 | | |
| App - Basic | 20 | | |
| App - Location | 10 | | |
| Integration | 10 | | |
| **TOTAL** | **55** | | |

### Issues Found
| # | Issue | Severity | Notes |
|---|-------|----------|-------|
| 1 | | | |
| 2 | | | |
| 3 | | | |

### Sign-off
- [ ] Tester: _________________
- [ ] Date: _________________
- [ ] Result: ☐ PASS / ☐ FAIL (with issues)

---

## 🚀 QUICK TEST COMMANDS

```bash
# Set location emulator tại POI 1
adb emu geo fix 105.550000 21.350000

# Set location POI 2
adb emu geo fix 105.555000 21.352000

# Xem log sync + tracking
adb logcat | grep -E "DiscoverPage|POI.*sync|Geofence|Entered|Exited"

# Kiểm tra SQLite trên device
adb shell run-as com.companyname.foodstreetguide cat /data/user/0/com.companyname.foodstreetguide/files/foodstreet.db
```

---

## ✅ READY TO TEST!

**Bắt đầu từ:**
1. Chạy XAMPP (Apache)
2. Cài đặt app mới nhất
3. Set location POI 1
4. Check từng item trong checklist!

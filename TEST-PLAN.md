# 📋 KẾ HOẠCH TEST FOODSTREET GUIDE

## 🎯 PHẠM VI TEST

### 1. GPS Tracking Theo Thời Gian Thực

#### Test Case 1.1: Khởi động tracking
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Mở app, bấm nút "Bắt đầu Tracking" | ✅ Nút chuyển sang "Dừng Tracking" (màu đỏ) |
| 2 | Kiểm tra notification | ✅ Hiển thị "Đang theo dõi vị trí..." |
| 3 | Kiểm tra `adb logcat` | ✅ Log: `[Track] Starting tracking...` |
| 4 | Di chuyển 10m | ✅ Vị trí trên map cập nhật |

#### Test Case 1.2: Background tracking
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Bật tracking | ✅ Đang tracking |
| 2 | Nhấn Home (app background) | ✅ Notification vẫn hiển thị |
| 3 | Di chuyển vật lý 50m | ✅ Geofence vẫn trigger khi vào vùng |
| 4 | Mở lại app | ✅ Map hiển thị vị trí mới nhất |

#### Test Case 1.3: Kill app
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Bật tracking | ✅ Đang tracking |
| 2 | Kill app (swipe away) | ✅ Notification biến mất |
| 3 | Check `adb logcat` | ✅ Log: `[App] OnSleep - App going to background` |
| 4 | Mở lại app | ✅ Tracking ở trạng thái OFF, cần bật lại |

#### Test Case 1.4: Pin & Độ chính xác
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Settings → GPS Interval | ✅ Thay đổi được 1-10 giây |
| 2 | Đặt 1 giây | ✅ Cập nhật vị trí mỗi 1s (pin tụt nhanh) |
| 3 | Đặt 10 giây | ✅ Cập nhật mỗi 10s (pin tiết kiệm) |
| 4 | Kiểm tra accuracy | ✅ Độ lệch < 10m so với vị trí thực |

---

### 2. Quản Lý Dữ Liệu POI

#### Test Case 2.1: Auto-sync khi mở app
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Xóa dữ liệu app (Settings → App → Clear Data) | ✅ App trống |
| 2 | Mở app (XAMPP đang chạy) | ✅ Auto sync tự động chạy |
| 3 | Kiểm tra `adb logcat` | ✅ Log: `[AutoSync] Pulling POIs from Web Admin...` |
| 4 | Đợi 5 giây | ✅ Danh sách POI hiển thị (đồng bộ từ web) |
| 5 | So sánh với Web Admin | ✅ Số lượng POI khớp nhau |

#### Test Case 2.2: Xem chi tiết POI
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Bấm vào marker POI trên map | ✅ Card hiển thị tên, địa chỉ |
| 2 | Kiểm tra thông tin | ✅ Có: Tên, Mô tả, Địa chỉ, Giờ mở cửa, Ảnh |
| 3 | Bấm "Chỉ đường" | ✅ Mở Google Maps với tọa độ POI |
| 4 | Kiểm tra link bản đồ | ✅ URL: `https://www.google.com/maps?q=lat,lng` |

#### Test Case 2.3: Thuyết minh TTS
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Vào Settings → TTS | ✅ Bật/Tắt được Narration |
| 2 | Chọn ngôn ngữ | ✅ Chuyển được EN/VI |
| 3 | Chọn tốc độ | ✅ 0.5x - 2.0x |
| 4 | Trigger POI | ✅ TTS phát đúng nội dung `narrationTextVi` |

---

### 3. Map View

#### Test Case 3.1: Hiển thị vị trí người dùng
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Mở app, cho phép GPS | ✅ Marker xanh hiển thị vị trí hiện tại |
| 2 | Di chuyển | ✅ Marker di chuyển theo |
| 3 | Tap vào marker | ✅ Hiển thị "Vị trí của bạn" |
| 4 | Kiểm tra accuracy | ✅ Vòng tròn xanh (radius) hiển thị |

#### Test Case 3.2: Hiển thị tất cả POI
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Mở app | ✅ Tất cả POI hiển thị marker |
| 2 | Zoom out | ✅ Marker cluster (nếu có nhiều POI gần nhau) |
| 3 | Tap POI marker | ✅ Card thông tin hiện lên |
| 4 | Kiểm tra màu marker | ✅ Màu theo category (Restaurant=đỏ, Cafe=vàng...) |

#### Test Case 3.3: Highlight POI gần nhất
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Bật tracking | ✅ Đang theo dõi |
| 2 | Di chuyển gần POI A (trong radius) | ✅ POI A marker TO HƠN/PULSE |
| 3 | Kiểm tra `adb logcat` | ✅ Log: `[OnNearestPOIChanged] Updated reference to: POI A` |
| 4 | Card hiển thị | ❌ **KHÔNG HIỆN** (chỉ khi trigger mới hiện) |

---

### 4. Geofence / Kích Hoạt Thuyết Minh

#### Test Case 4.1: Trigger đơn giản
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Tắt tracking | ✅ Tracking off |
| 2 | Bật lại tracking | ✅ Engine re-initialized |
| 3 | Di chuyển vào POI A | ✅ Log: `[OnPOIEntered] TRIGGERED: POI A` |
| 4 | Card hiển thị | ✅ Card POI A hiện lên với đầy đủ thông tin |
| 5 | TTS phát | ✅ Phát narration (nếu bật) |
| 6 | Di chuyển ra khỏi POI A | ✅ Log: `[OnPOIExited] EXITED POI: POI A` |
| 7 | Card ẩn | ✅ Card biến mất |

#### Test Case 4.2: Cooldown (chống spam)
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Trigger POI A | ✅ Card hiện, TTS phát |
| 2 | Tắt tracking, bật lại ngay | ✅ Không trigger lại ngay |
| 3 | Kiểm tra log | ✅ Log: `[Geofence] SKIP POI A (cooldown: XXs ago)` |
| 4 | Đợi 5 phút | ⏱️ Đợi cooldown hết |
| 5 | Trigger lại | ✅ Trigger được lại sau 5 phút |

#### Test Case 4.3: Chuyển POI (Smart Mode)
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Trigger POI A | ✅ Card A hiện |
| 2 | Di chuyển sang POI B (gần hơn) | ✅ Log: `[OnNearestPOIChanged] Updated reference to: POI B` |
| 3 | Card A ẩn, Card B hiện? | ✅ KHÔNG - chỉ khi trigger mới hiện |
| 4 | Trigger POI B | ✅ Card B hiện, Card A ẩn |
| 5 | Quay lại POI A (trong cooldown) | ❌ Không trigger A (cooldown 5 phút) |
| 6 | Trigger POI C (gần nhất khác) | ✅ Card C hiện |

#### Test Case 4.4: Radius & Priority
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Settings → Geofence Radius | ✅ Thay đổi được 10-1000m |
| 2 | Đặt radius 50m | ✅ Chỉ trigger khi vào trong 50m |
| 3 | Đặt priority cao (3) cho POI X | ✅ POI X ưu tiên trigger trước |

---

### 5. Trường Hợp Thực Tế & Lỗi

#### Test Case 5.1: Mất kết nối Web
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Tắt XAMPP Apache | ✅ Web server down |
| 2 | Mở app | ✅ App dùng dữ liệu SQLite local |
| 3 | Kiểm tra `adb logcat` | ✅ Log: `[AutoSync] Error: Cannot connect to Web Admin` |
| 4 | Tracking vẫn hoạt động | ✅ Geofence vẫn trigger với POI local |

#### Test Case 5.2: GPS yếu / Mất GPS
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Bật tracking trong nhà (GPS yếu) | ✅ Sử dụng network/wifi location |
| 2 | Tắt hoàn toàn GPS | ⚠️ Hiển thị "Không thể lấy vị trí" |
| 3 | Bật lại GPS | ✅ Tự động resume tracking |

#### Test Case 5.3: POI trùng địa chỉ
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Web Admin → tạo POI với địa chỉ đã có | ❌ Báo lỗi: "Địa điểm này trùng với quán..." |
| 2 | Xem danh sách trùng | ✅ Vào "POI Trùng lặp" để xem |
| 3 | Gộp nhóm trùng | ✅ Xóa POI trùng, giữ 1 POI |

#### Test Case 5.4: App crash khi đang tracking
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Bật tracking | ✅ Đang tracking |
| 2 | Force kill app (từ Settings) | ✅ Notification biến mất |
| 3 | Mở lại app | ✅ Tracking ở trạng thái OFF |
| 4 | Không có lỗi crash | ✅ App mở bình thường |

#### Test Case 5.5: Thay đổi tab giữa Map và Khám phá
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Bật tracking ở tab Map | ✅ Đang tracking |
| 2 | Chuyển sang tab Khám phá | ✅ Tracking vẫn tiếp tục |
| 3 | Quay lại tab Map | ✅ Vị trí cập nhật liên tục |
| 4 | Trigger POI | ✅ Vẫn trigger bình thường |

#### Test Case 5.6: Nhiều POI gần nhau
| Bước | Thao tác | Kết quả mong đợi |
|------|---------|-----------------|
| 1 | Tạo 3 POI trong bán kính 100m | ✅ 3 POI trên map |
| 2 | Đứng ở trung tâm | ✅ Log: `[Geofence] Inside 3 POIs` |
| 3 | Trigger | ✅ Trigger POI gần nhất (nearest) |
| 4 | Cooldown POI đó | ✅ Log: `[Geofence] SKIP POI X (cooldown)` |
| 5 | Trigger tiếp | ✅ Trigger POI gần nhất tiếp theo |

---

## 🛠️ CÔNG CỤ TEST

### ADB Commands
```bash
# Xem log
adb logcat -s "[Geofence]" "[Track]" "[OnPOIEntered]" "[OnPOIExited]" "[App]"

# Send mock location
adb shell am startservice -a theappninjas.gpsjoystick.TELEPORT --ef lat 10.762622 --ef lng 106.660172

# Clear app data
adb shell pm clear com.companyname.foodstreetguide

# Install APK
adb install -r com.companyname.foodstreetguide-Signed.apk
```

### Mock GPS Apps
- **Fake GPS Location** (Google Play)
- **GPS JoyStick** (cho Android emulator)

### Test Locations (Real POI)
| POI | Lat | Lng | Test Scenario |
|-----|-----|-----|---------------|
| Chợ Bến Thành | 10.7725 | 106.6980 | Di chuyển vào/ra |
| Phố đi bộ Nguyễn Huệ | 10.7753 | 106.7009 | Nhiều POI gần nhau |
| Bitexco | 10.7716 | 106.7042 | Cooldown test |

---

## ✅ CHECKLIST TRƯỚC KHI RELEASE

- [ ] GPS tracking hoạt động foreground + background
- [ ] Kill app đúng cách (tracking dừng)
- [ ] Auto-sync từ web khi mở app
- [ ] Geofence trigger đúng ( vào vùng → hiện card → TTS)
- [ ] Cooldown 5 phút hoạt động
- [ ] Smart mode: nearest + cooldown per POI
- [ ] Không trigger khi tắt tracking bật lại ngay
- [ ] Card ẩn khi ra khỏi vùng
- [ ] Switch tab không mất tracking
- [ ] Offline mode hoạt động (SQLite local)
- [ ] Không crash khi GPS yếu/mất
- [ ] POI trùng được phát hiện và cảnh báo
- [ ] TTS phát đúng ngôn ngữ và tốc độ
- [ ] Ảnh POI hiển thị đúng
- [ ] Chỉ đường mở Google Maps đúng tọa độ

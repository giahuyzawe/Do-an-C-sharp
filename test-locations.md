# 🗺️ TEST LOCATIONS - TP.HCM Real POIs

## 📍 DANH SÁCH POI THỰC TẾ (Có trong Web Admin)

### 1. Bánh Mì Huỳnh Hoa ⭐⭐⭐
- **Địa chỉ:** 26 Lê Thị Riêng, P. Bến Thành, Q.1
- **Tọa độ:** `10.7701, 106.6923`
- **Radius:** 50m
- **Category:** Street Food
- **Mô tả:** Bánh mì nổi tiếng nhất Sài Gòn, giá ~65k/ổ

### 2. Phở Lệ ⭐⭐⭐
- **Địa chỉ:** 303 Võ Văn Tần, P.5, Q.3  
- **Tọa độ:** `10.7545, 106.6677`
- **Radius:** 100m
- **Category:** Restaurant
- **Mô tả:** Phở bò tái chín nổi tiếng, nước dùng đậm đà

### 3. The Coffee House ☕
- **Địa chỉ:** 91 Đồng Khởi, P. Bến Nghé, Q.1
- **Tọa độ:** `10.7769, 106.7009`  
- **Radius:** 150m
- **Category:** Cafe
- **Mô tả:** Chuỗi cafe hiện đại, wifi mạnh

### 4. Chợ Bến Thành 🏪
- **Địa chỉ:** Lê Lợi, P. Bến Thành, Q.1
- **Tọa độ:** `10.7720, 106.6983`
- **Radius:** 300m (rộng)
- **Category:** Market
- **Mô tả:** Biểu tượng Sài Gòn, ăn uống + mua sắm

### 5. Bún Bò Huế Đông Ba 🍜
- **Địa chỉ:** 110A Nguyễn Du, P. Bến Thành, Q.1
- **Tọa độ:** `10.7760, 106.6955`
- **Radius:** 100m
- **Category:** Restaurant  
- **Mô tả:** Bún bò Huế chuẩn vị, cay nồng

### 6. Cơm Tấm Ba Ghiền 🍚
- **Địa chỉ:** 84 Đặng Văn Ngữ, P.10, Q.Phú Nhuận
- **Tọa độ:** `10.8010, 106.6802`
- **Radius:** 100m
- **Category:** Restaurant
- **Mô tả:** Sườn nướng to, đặc sản Sài Gòn

### 7. Phố Đi Bộ Nguyễn Huệ 🚶
- **Địa chỉ:** Nguyễn Huệ, Q.1
- **Tọa độ:** `10.7765, 106.7019`
- **Radius:** 500m (dài)
- **Category:** Park/Walking
- **Mô tả:** Phố đi bộ sầm uất, nhiều quán cafe view đẹp

---

## 🧪 CÁCH TEST TRACKING

### Cách 1: Dùng Android Emulator + Mock Location

#### Test Scenario 1: Bắt đầu từ xa → Vào quán
```
Bước 1: Set location xa quán  (10.7800, 106.7100) - Chợ Bến Thành area
Bước 2: Di chuyển vào:      (10.7701, 106.6923) - Bánh Mì Huỳnh Hoa
Kỳ vọng: Hiện POI card + TTS narration
```

#### Test Scenario 2: Giữa 2 quán (Test nearest priority)
```
Vị trí: 10.7730, 106.6950 (Giữa Chợ Bến Thành và Bánh Mì)
→ Khoảng cách:
  - Đến Chợ Bến Thành: ~120m 
  - Đến Bánh Mì H.Hoa: ~280m
Kỳ vọng: Trigger Chợ Bến Thành (gần hơn)
```

#### Test Scenario 3: Đổi quán khi di chuyển
```
Lộ trình test:
1. (10.7765, 106.7019) - Nguyễn Huệ → Trigger POI #7
2. (10.7720, 106.6983) - Chợ Bến Thành → Trigger POI #4  
3. (10.7701, 106.6923) - Bánh Mì H.Hoa → Trigger POI #1

Kỳ vọng: Mỗi lần vào radius quán mới, POI card đổi theo
```

### Cách 2: Dùng Fake GPS App (Trên điện thoại thật)

**App đề xuất:**
- Fake GPS Location (Google Play)
- GPS Joystick

**Bước setup:**
1. Cài Fake GPS app
2. Vào Developer Options → Mock Location App → Chọn Fake GPS
3. Chọn tọa độ từ list trên
4. Mở FoodStreetGuide app

---

## 📱 LỆNH ADB TEST

### Set Mock Location:
```bash
# Bánh Mì Huỳnh Hoa
adb shell am startservice -a theappninjas.gpsjoystick.TELEPORT --ef lat 10.7701 --ef long 106.6923

# Phở Lệ
adb shell am startservice -a theappninjas.gpsjoystick.TELEPORT --ef lat 10.7545 --ef long 106.6677

# Chợ Bến Thành
adb shell am startservice -a theappninjas.gpsjoystick.TELEPORT --ef lat 10.7720 --ef long 106.6983
```

### Xem log khi test:
```bash
adb logcat -s "Geofence" "OnPOIEntered" "MainPage" "FoodStreetGuide" *:S
```

---

## ⚠️ LƯU Ý QUAN TRỌNG

### Trước khi test:
1. **Clear app data** để xoá POI cũ (seed data 3 quán)
2. **Mở app** → Auto sync từ web (sẽ có 7 quán)
3. Kiểm tra log: `[OnAppearing] Loaded 7 POIs from database`

### Cooldown 5 phút:
- Nếu đã trigger 1 quán, phải đợi 5 phút hoặc ra khỏi radius mới trigger lại
- Hoặc restart app

### Test thứ tự:
1. Test đơn: Mỗi lần 1 quán → Xác nhận POI card hiện đúng
2. Test liên tiếp: Di chuyển qua 3-4 quán → Xác nhận đổi POI card
3. Test gần nhất: Đứng giữa 2 quán → Xác nhận trigger quán gần hơn

---

## 🎯 KẾT QUẢ MONG ĐỢI

Khi đứng tại tọa độ quán, app sẽ:
1. **Hiện POI card** với tên quán đúng
2. **Phát TTS** narration của quán đó
3. **Cập nhật map** highlight quán đó
4. **Cooldown 5 phút** trước khi trigger lại cùng quán

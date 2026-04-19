# 🗺️ LỘ TRÌNH TEST 20 POI - VĨNH PHÚC FOOD TOUR

## 📍 Tổng quan
- **Khu vực**: Trung tâm TP. Vĩnh Phúc
- **Số điểm**: 20 POI
- **Khoảng cách trung bình**: 150-300m giữa các điểm
- **Thời gian đi bộ ước tính**: 30-45 phút

---

## 🚶 LỘ TRÌNH CHI TIẾT (Test từng bước)

### **ĐIỂM 1: Phở Gà Vĩnh Phúc** 🍜
- **Tọa độ**: `21.350000, 105.550000`
- **Địa chỉ**: 12 Nguyễn Văn Linh, P. Hùng Vương
- **Mô tả**: Bắt đầu tour ẩm thực với phở gà đặc trưng
- **Test**: Set location → App hiện thông báo "Gần Phở Gà"

### **ĐIỂM 2: Bún Chả Vĩnh Yên** 🥢
- **Tọa độ**: `21.352000, 105.555000`
- **Cách điểm 1**: ~200m
- **Test**: Di chuyển → App phát hiện điểm mới

### **ĐIỂM 3: Cà Phê Highlands** ☕
- **Tọa độ**: `21.348000, 105.553000`
- **Cách điểm 2**: ~180m
- **Test**: Nghỉ giải lao, check-in

### **ĐIỂM 4: Chả Cá Lã Vọng** 🐟
- **Tọa độ**: `21.355000, 105.548000`
- **Cách điểm 3**: ~250m

### **ĐIỂM 5: Bánh Mì Phượng** 🥖
- **Tọa độ**: `21.347000, 105.557000`
- **Cách điểm 4**: ~220m

### **ĐIỂM 6: Lẩu Bò Nhúng Dấm** 🍲
- **Tọa độ**: `21.353000, 105.560000`
- **Cách điểm 5**: ~280m

### **ĐIỂM 7: Bún Riêu Cua** 🦀
- **Tọa độ**: `21.345000, 105.552000`
- **Cách điểm 6**: ~200m

### **ĐIỂM 8: Bánh Cuốn Thanh Trì** 🌯
- **Tọa độ**: `21.349000, 105.558000`
- **Cách điểm 7**: ~150m

### **ĐIỂM 9: Cháo Lòng** 🥣
- **Tọa độ**: `21.351000, 105.562000`
- **Cách điểm 8**: ~160m

### **ĐIỂM 10: Bún Thang** 🍜
- **Tọa độ**: `21.354000, 105.565000`
- **Cách điểm 9**: ~180m

### **ĐIỂM 11: Nem Nướng Nha Trang** 🍢
- **Tọa độ**: `21.356000, 105.568000`
- **Cách điểm 10**: ~200m

### **ĐIỂM 12: Bánh Xèo Miền Trung** 🥞
- **Tọa độ**: `21.358000, 105.570000`
- **Cách điểm 11**: ~150m

### **ĐIỂM 13: Trà Sữa Gong Cha** 🧋
- **Tọa độ**: `21.346000, 105.548000`
- **Cách điểm 12**: ~300m

### **ĐIỂM 14: Bánh Tráng Trộn** 🥗
- **Tọa độ**: `21.344000, 105.555000`
- **Cách điểm 13**: ~180m

### **ĐIỂM 15: Gỏi Cuốn Tôm Thịt** 🍤
- **Tọa độ**: `21.342000, 105.560000`
- **Cách điểm 14**: ~170m

### **ĐIỂM 16: Bánh Canh Cua** 🍜
- **Tọa độ**: `21.340000, 105.565000`
- **Cách điểm 15**: ~200m

### **ĐIỂM 17: Cơm Gà Hải Nam** 🍚
- **Tọa độ**: `21.338000, 105.570000`
- **Cách điểm 16**: ~220m

### **ĐIỂM 18: Bún Bò Huế** 🌶️
- **Tọa độ**: `21.336000, 105.575000`
- **Cách điểm 17**: ~200m

### **ĐIỂM 19: Sinh Tố Bơ Đà Lạt** 🥑
- **Tọa độ**: `21.334000, 105.580000`
- **Cách điểm 18**: ~250m

### **ĐIỂM 20: Chè Thập Cẩm** 🍨
- **Tọa độ**: `21.332000, 105.585000`
- **Cách điểm 19**: ~200m
- **Kết thúc tour**: Điểm tráng miệng hoàn hảo!

---

## 🎯 QUY TRÌNH TEST

### **Bước 1: Khởi động**
```
1. Mở Android Emulator
2. Set location: 21.350000, 105.550000 (Điểm 1)
3. Mở app FoodStreetGuide
4. Đợi app sync 20 POIs từ Web Admin
5. Kiểm tra map hiển thị 20 pin
```

### **Bước 2: Test đi bộ**
```
FOR i = 1 TO 20:
    1. Set emulator location tại POI thứ i
    2. Đợi 5-10 giây cho app cập nhật GPS
    3. Kiểm tra:
       - App hiện "Gần [Tên nhà hàng]"? ✅
       - POI card hiện thông tin đúng? ✅
       - Analytics gửi lên Web Admin? ✅
    4. Click "Check-in" (nếu có QR)
    5. Chuyển sang POI i+1
```

### **Bước 3: Kiểm tra Web Admin**
- Mở `http://localhost/foodtour-admin/statistics.php`
- Kiểm tra:
  - DAU (Daily Active Users) tăng
  - POI Views có đủ 20 lượt xem
  - Check-in count tăng (nếu test QR)

---

## 📱 LỆNH ADB HỮU ÍCH

### Set location nhanh:
```bash
# Điểm 1
adb emu geo fix 105.550000 21.350000

# Điểm 2
adb emu geo fix 105.555000 21.352000

# Điểm 3
adb emu geo fix 105.553000 21.348000

# ... và tiếp tục cho 20 điểm
```

### Xem log tracking:
```bash
adb logcat | grep -E "POI Entered|POI Exited|Nearest|Geofence"
```

---

## ✅ CHECKLIST TEST

- [ ] App sync 20 POIs từ Web Admin
- [ ] Map hiển thị 20 pin nhà hàng
- [ ] Passive tracking hoạt động (auto-start)
- [ ] Geofence phát hiện khi đến gần POI
- [ ] Thông báo "Gần nhà hàng" hiện đúng
- [ ] POI card hiển thị thông tin chi tiết
- [ ] Analytics gửi đúng (app_visit, poi_view)
- [ ] Web Admin cập nhật thống kê
- [ ] QR Check-in hoạt động (nếu test)

---

## 🗺️ BẢN ĐỒ TRỰC QUAN

```
Điểm 1 (21.350, 105.550) ──200m──▶ Điểm 2 (21.352, 105.555)
    │                                      │
    │                                      │
  250m                                 180m
    │                                      │
    ▼                                      ▼
Điểm 4 (21.355, 105.548) ◀──180m── Điểm 3 (21.348, 105.553)
```

---

## 📊 THÔNG TIN BỔ SUNG

| Thông số | Giá trị |
|----------|---------|
| Tổng số POI | 20 |
| Tọa độ Lat | 21.332 - 21.358 |
| Tọa độ Long | 105.548 - 105.585 |
| Bán kính vùng test | ~3km |
| Khoảng cách trung bình | 200m |
| Thời gian test ước tính | 30-45 phút |

---

**Sẵn sàng test! 🚀**
Bắt đầu từ Điểm 1 và đi theo lộ trình nhé!

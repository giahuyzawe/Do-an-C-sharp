# 🧪 TEST PLAN - Food Tour App with Web Admin API

## 📋 Tổng quan

Test kết nối App với Web Admin qua API để đồng bộ dữ liệu real-time.

---

## 🔧 Chuẩn bị

### 1. Mở XAMPP
- [ ] Start Apache
- [ ] Verify: `http://localhost/foodtour-admin/test.php` hoạt động

### 2. Start ngrok
```bash
cd c:\xampp\htdocs\foodtour-admin
start-ngrok.bat
```
- [ ] Copy ngrok URL (ví dụ: `https://xxx.ngrok-free.dev`)
- [ ] Cập nhật trong `ApiService.cs` nếu URL thay đổi

### 3. Kiểm tra API
Mở browser:
```
https://xxx.ngrok-free.dev/foodtour-admin/test-api.html
```
- [ ] Test Files: All green
- [ ] Test Load POIs: Hiển thị danh sách
- [ ] Test Analytics: Có dữ liệu

---

## 🚀 Deploy App lên Emulator

### Chạy script deploy
```bash
cd C:\Users\PC\FoodStreetGuide
deploy-emulator.bat
```

- [ ] Build APK thành công
- [ ] Emulator khởi động
- [ ] App cài đặt thành công
- [ ] App tự động mở

---

## 📝 Test Cases

### TC1: App Launch - Gửi App Visit
**Mục tiêu:** Khi mở app, gửi analytics lên Web Admin

**Steps:**
1. Mở app trên emulator
2. Đợi 3-5 giây để khởi động
3. Kiểm tra Web Admin Dashboard

**Expected Result:**
- [ ] Dashboard hiển thị DAU tăng 1
- [ ] Trang Statistics hiển thị "App Visits: 1"
- [ ] Debug log: `[App] Analytics sent to Web Admin`

**Verify:**
```
URL: https://xxx.ngrok-free.dev/foodtour-admin/statistics.php
Check: Hôm nay → DAU = 1
```

---

### TC2: View POI - Gửi POI View
**Mục tiêu:** Khi xem chi tiết nhà hàng, gửi poi_view lên Web Admin

**Steps:**
1. Từ màn hình chính, click vào nhà hàng "Phở Gà Vĩnh Phúc"
2. Đợi trang chi tiết load xong
3. Back về Dashboard
4. Kiểm tra Web Admin

**Expected Result:**
- [ ] POI Views tăng 1 trong Statistics
- [ ] "Phở Gà Vĩnh Phúc" hiển thị visit count tăng
- [ ] Debug log: `[POIDetailPage] POI view sent to Web Admin: 1`

**Verify:**
```
URL: https://xxx.ngrok-free.dev/foodtour-admin/statistics.php
Check: Hôm nay → Lượt xem POI = 1
```

---

### TC3: QR Check-in - Gửi Check-in
**Mục tiêu:** Khi quét QR, validate qua API và gửi check-in

**Pre-condition:**
- Tạo QR code trong Web Admin trước:
```
URL: https://xxx.ngrok-free.dev/foodtour-admin/qr-generator.php
Chọn: Phở Gà Vĩnh Phúc
Tạo QR → Tải ảnh về máy tính
```

**Steps:**
1. Trong app, nhấn nút "Quét QR" (hoặc vào tab QR)
2. Giả lập quét QR code đã tải về
3. Hoặc nhập token thủ công (nếu có input)

**Expected Result:**
- [ ] Hiển thị "Check-in thành công!"
- [ ] Hiển thị số thứ tự check-in (ví dụ: "Bạn là khách thứ 5")
- [ ] Chuyển đến trang chi tiết nhà hàng
- [ ] Check-in QR tăng 1 trong Web Admin Statistics
- [ ] Debug log: `[QRScanPage] Check-in sent to Web Admin: 1`

**Verify:**
```
URL: https://xxx.ngrok-free.dev/foodtour-admin/statistics.php
Check: Hôm nay → Check-in QR = 1
```

---

### TC4: Load POIs from Web Admin
**Mục tiêu:** App lấy danh sách nhà hàng từ API (nếu có implement)

**Note:** Hiện tại app dùng local database. API `/api/get-pois.php` đã sẵn sàng để tích hợp sau.

**Verify API hoạt động:**
```bash
curl https://xxx.ngrok-free.dev/foodtour-admin/api/get-pois.php
```

Expected: JSON response với danh sách nhà hàng đã duyệt.

---

### TC5: Offline Mode (Error Handling)
**Mục tiêu:** App hoạt động bình thường khi mất kết nối

**Steps:**
1. Tắt WiFi trên emulator (Settings → Network)
2. Mở app
3. Xem nhà hàng
4. Bật lại WiFi

**Expected Result:**
- [ ] App vẫn mở bình thường
- [ ] Không crash
- [ ] Debug log: `[App] API error: Network unreachable`
- [ ] Analytics được lưu local, gửi lại khi có mạng (nếu implement retry)

---

## 🐞 Bug Report Template

Nếu phát hiện lỗi, ghi chú theo mẫu:

```
**Bug ID:** BUG-001
**Title:** [Mô tả ngắn]
**Severity:** [Critical/High/Medium/Low]
**Steps to Reproduce:**
1. 
2. 
3. 

**Expected Result:**
[What should happen]

**Actual Result:**
[What actually happened]

**Screenshots/Logs:**
[Attach images or debug logs]

**Environment:**
- Emulator: [Tên device]
- Android Version: [e.g., 13]
- App Version: [e.g., 1.0.0]
- ngrok URL: [URL]
```

---

## ✅ Test Checklist Summary

| # | Test Case | Status | Notes |
|---|-----------|--------|-------|
| 1 | Deploy lên emulator | ⬜ Pass / ⬜ Fail | |
| 2 | App Launch → App Visit | ⬜ Pass / ⬜ Fail | Check Dashboard |
| 3 | View POI → POI View | ⬜ Pass / ⬜ Fail | Check Statistics |
| 4 | QR Check-in → Check-in | ⬜ Pass / ⬜ Fail | Cần QR code |
| 5 | API get-pois hoạt động | ⬜ Pass / ⬜ Fail | Test bằng curl |
| 6 | Offline mode | ⬜ Pass / ⬜ Fail | Tắt WiFi test |

---

## 🎯 Success Criteria

- ✅ App deploy thành công lên emulator
- ✅ App gửi analytics đến Web Admin real-time
- ✅ Web Admin hiển thị đúng số liệu (DAU, POI Views, Check-ins)
- ✅ QR check-in hoạt động qua API
- ✅ App không crash khi mất mạng

---

## 📞 Hỗ trợ

Nếu gặp vấn đề:
1. Kiểm tra ngrok URL còn active không
2. Kiểm tra XAMPP Apache đang chạy
3. Xem debug log trong Visual Studio Output
4. Test API bằng browser trước

---

**Ngày test:** ___/___/___  
**Người test:** _______________  
**Kết quả tổng thể:** ⬜ PASS / ⬜ FAIL

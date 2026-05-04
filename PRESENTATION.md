# 📱 FOOD STREET GUIDE - BÀI THUYẾT TRÌNH

## 🎯 GIỚI THIỆU TỔNG QUAN

**Food Street Guide** là hệ thống hướng dẫn ẩm thực đường phố thông minh kết hợp công nghệ định vị GPS và QR Code, giúp du khách khám phá các điểm ẩm thực đặc sắc tại Vĩnh Phúc.

---

## 📱 PHẦN 1: MOBILE APP

### 1.1 Tổng quan ứng dụng

| Thông tin | Chi tiết |
|-----------|----------|
| **Nền tảng** | Android (có thể mở rộng iOS) |
| **Công nghệ** | .NET MAUI 9.0 |
| **Database** | SQLite (offline-first) |
| **Ngôn ngữ** | Tiếng Việt & English |

### 1.2 Các tính năng chính

#### 🗺️ **Khám phá & Định vị**
- **Bản đồ tương tác**: Hiển thị tất cả điểm ẩm thực (POI) trên bản đồ
- **Vị trí real-time**: Theo dõi vị trí du khách liên tục
- **Geofence**: Tự động phát hiện khi vào/vào vùng nhà hàng (bán kính tùy chỉnh)
- **Tìm kiếm**: Tìm nhà hàng theo tên hoặc món ăn

#### 📍 **Check-in Thông Minh**
- **QR Check-in**: Quét mã QR để check-in nhanh chóng
- **Auto Check-in**: Tự động check-in khi du khách đến gần POI
- **Không giới hạn**: Mỗi lần quét đều được ghi nhận

#### 📝 **Đánh giá & Review**
- **Đánh giá sao**: 1-5 sao cho mỗi điểm ẩm thực
- **Viết nhận xét**: Chia sẻ trải nghiệm cá nhân
- **Đồng bộ**: Review được đồng bộ với Web Admin
- **Xem đánh giá**: Đọc review từ du khách khác

#### 🎵 **Trải nghiệm đa phương tiện**
- **Audio guide**: Nghe giới thiệu bằng Tiếng Việt hoặc English
- **Text-to-Speech**: Tự động đọc mô tả
- **Hình ảnh**: Xem ảnh món ăn, không gian quán

#### 🔄 **Offline Mode**
- **Làm việc offline**: Toàn bộ dữ liệu POI lưu local
- **Sync khi có mạng**: Tự động đồng bộ khi online
- **Tiết kiệm data**: Không cần mạng liên tục

---

## 💻 PHẦN 2: WEB ADMIN

### 2.1 Tổng quan hệ thống quản lý

| Thông tin | Chi tiết |
|-----------|----------|
| **Nền tảng** | PHP + JSON (file-based) |
| **Server** | XAMPP/Apache |
| **UI** | Bootstrap 5 |
| **Xác thực** | Role-based (Admin, Owner, Staff) |

### 2.2 Các module chính

#### 📊 **Dashboard**
- **Thống kê real-time**: DAU, views, check-ins
- **Top POI**: Các điểm được xem/check-in nhiều nhất
- **Biểu đồ**: Xu hướng theo ngày/tuần/tháng

#### 🏪 **Quản lý POI (Nhà hàng)**
- **CRUD đầy đủ**: Thêm, sửa, xóa, xem chi tiết
- **Thông tin chi tiết**:
  - Tên (Tiếng Việt & English)
  - Mô tả chi tiết
  - Địa chỉ, số điện thoại
  - Giờ mở cửa
  - Tọa độ GPS
  - Bán kính check-in (m)
  - Độ ưu tiên hiển thị
- **Trạng thái**: Approved, Pending, Rejected
- **Upload ảnh/audio**: Quản lý media

#### 📈 **Analytics & Thống kê**
- **Daily Active Users (DAU)**: Số device unique mỗi ngày
- **Page Views**: Lượt xem POI
- **Check-ins**: Số lần check-in thành công
- **QR Scans**: Thống kê quét mã QR
- **Heatmap**: Dữ liệu vị trí du khách

#### 💬 **Quản lý Review**
- **Duyệt review**: Approve/reject đánh giá
- **Moderation**: Xóa review không phù hợp
- **Phản hồi**: Trả lời đánh giá của khách

#### 🔲 **QR Code System**
- **Tạo QR**: Tự động tạo mã QR cho mỗi POI
- **In/Export**: Xuất QR để in ấn
- **Tracking**: Theo dõi lượt quét mã

#### 👤 **User Management**
- **Phân quyền**:
  - **Admin**: Full access
  - **Restaurant Owner**: Chỉ POI của mình
  - **Staff**: View only
- **Quản lý tài khoản**: Tạo, khóa, reset password

#### 🎵 **Audio Management**
- **Upload audio**: File mp3/wav
- **TTS integration**: Text-to-Speech tự động
- **Auto-play setting**: Bật/tắt tự động phát

---

## 🏗️ PHẦN 3: KIẾN TRÚC HỆ THỐNG

### 3.1 Sơ đồ tổng quan

```
┌─────────────────┐     HTTP/JSON      ┌─────────────────┐
│   MOBILE APP    │ ◄────────────────► │   WEB SERVER    │
│  (.NET MAUI)    │                    │  (PHP + Apache) │
│                 │                    │                 │
│ • SQLite (local)│                    │ • JSON files    │
│ • GPS tracking  │                    │ • API endpoints │
│ • Offline mode  │                    │ • Admin panel   │
└─────────────────┘                    └─────────────────┘
         │                                      │
         │         QR CODE REDIRECT            │
         │◄───────────────────────────────────┤
         │                                      │
    ┌────▼────┐                           ┌─────▼────┐
    │ QR SCAN │                           │ DATABASE │
    │ CHECK-IN│                           │ (JSON)   │
    └─────────┘                           └──────────┘
```

### 3.2 Data Flow

#### Sync POI Data
```
Web Admin ──► JSON files ──► API get-pois.php ──► Mobile App ──► SQLite
```

#### Check-in Process
```
Mobile QR Scan ──► API check-qr.php ──► Validate ──► Record Analytics
                              │
                              ▼
                        Update counts
                        Return POI info
```

#### Review Flow
```
Mobile Review ──► API post-review.php ──► Save to JSON ──► Sync to App
```

---

## 🛠️ PHẦN 4: CÔNG NGHỆ SỬ DỤNG

### 4.1 Mobile App

| Layer | Technology |
|-------|-----------|
| **Framework** | .NET MAUI 9.0 |
| **Language** | C# |
| **Database** | SQLite + SQLitePCLRaw |
| **Location** | Android Location Services |
| **QR Scan** | ZXing.Net |
| **HTTP** | System.Net.Http |

### 4.2 Web Admin

| Layer | Technology |
|-------|-----------|
| **Backend** | PHP 8.x |
| **Data** | JSON files (file-based DB) |
| **Frontend** | Bootstrap 5, jQuery |
| **Charts** | Chart.js |
| **Icons** | Bootstrap Icons |
| **Server** | Apache (XAMPP) |

### 4.3 APIs

| Endpoint | Chức năng |
|----------|-----------|
| `get-pois.php` | Lấy danh sách POI |
| `get-reviews.php` | Lấy review của POI |
| `post-review.php` | Gửi review mới |
| `post-analytics.php` | Ghi nhận analytics |
| `check-qr.php` | Validate & check-in QR |

---

## 📋 PHẦN 5: QUY TRÌNH SỬ DỤNG

### 5.1 Đối với Du Khách

1. **Tải app**: Download APK từ website
2. **Cài đặt**: Allow unknown sources → Install
3. **Mở app**: Cho phép GPS
4. **Khám phá**: Xem bản đồ các điểm ẩm thực
5. **Đến nhà hàng**: App tự động phát hiện
6. **Check-in**: Quét QR hoặc auto check-in
7. **Trải nghiệm**: Nghe audio, xem mô tả
8. **Đánh giá**: Đánh sao + viết review

### 5.2 Đối với Chủ Nhà Hàng

1. **Đăng ký**: Liên hệ Admin tạo tài khoản
2. **Đăng nhập**: Vào Web Admin
3. **Thêm POI**: Nhập thông tin nhà hàng
4. **Upload media**: Ảnh, audio
5. **Chờ duyệt**: Admin approve
6. **Nhận QR**: Tải mã QR để đặt tại quán
7. **Theo dõi**: Xem thống kê lượt xem/check-in

### 5.3 Đối với Admin

1. **Quản lý POI**: Duyệt, chỉnh sửa, xóa
2. **Quản lý User**: Phân quyền, tạo tài khoản
3. **Xem Analytics**: Thống kê toàn hệ thống
4. **Moderation**: Duyệt review, xử lý báo cáo
5. **QR Management**: Tạo/regenerate mã QR

---

## 🎯 PHẦN 6: ĐIỂM NỔI BẬT

### 6.1 Tính năng độc đáo

✅ **Geofence thông minh**: Auto check-in khi đến gần
✅ **Offline-first**: Hoạt động không cần mạng
✅ **Real-time tracking**: Vị trí du khách real-time
✅ **QR System**: Mỗi POI có QR riêng
✅ **Audio Guide**: Trải nghiệm nghe thông minh

### 6.2 Ưu điểm kỹ thuật

✅ **Lightweight**: Không cần database server (JSON-based)
✅ **Easy deploy**: Chỉ cần XAMPP/Apache
✅ **Scalable**: Có thể nâng cấp lên MySQL nếu cần
✅ **Cross-platform**: .NET MAUI dễ mở rộng iOS
✅ **Secure**: Role-based access control

---

## 📊 PHẦN 7: KẾT QUẢ & HIỆU QUẢ

### 7.1 Số liệu hiện tại

| Metric | Giá trị |
|--------|---------|
| **Số POI** | 20 điểm ẩm thực |
| **Ngôn ngữ** | 2 (Vi + En) |
| **Check-in** | Không giới hạn |
| **Review** | Real-time sync |

### 7.2 Lợi ích mang lại

- **Du khách**: Khám phá dễ dàng, trải nghiệm phong phú
- **Chủ nhà hàng**: Tăng visibility, thu hút khách
- **Quản lý**: Dữ liệu analytics đầy đủ

---

## 🚀 PHẦN 8: HƯỚNG PHÁT TRIỂN

### 8.1 Tính năng tương lai

🔮 **AI Recommendation**: Gợi ý nhà hàng dựa trên sở thích
🔮 **Social Share**: Chia sẻ lên Facebook, Instagram
🔮 **Voucher Integration**: Tích hợp mã giảm giá
🔮 **Multi-city**: Mở rộng nhiều tỉnh thành
🔮 **iOS Version**: Phát triển app cho iPhone

### 8.2 Cải tiến kỹ thuật

🔮 **MySQL Migration**: Khi dữ liệu lớn
🔮 **Cloud Hosting**: AWS/VPS cho production
🔮 **Push Notification**: Thông báo real-time
🔮 **Advanced Analytics**: Heatmap, user journey

---

## 🎬 KẾT LUẬN

**Food Street Guide** là giải pháp công nghệ toàn diện cho du lịch ẩm thực, kết hợp:
- 📱 Mobile app tiện lợi cho du khách
- 💻 Web admin mạnh mẽ cho quản lý
- 🔗 Hệ thống QR và GPS thông minh
- 📊 Analytics chi tiết

**Sẵn sàng triển khai và mở rộng!**

---

## 📞 THÔNG TIN LIÊN HỆ

- **Repository**: https://github.com/giahuyzawe/Do-an-C-sharp
- **Tech Stack**: .NET MAUI, PHP, SQLite, Bootstrap
- **Platform**: Android, Web

---

*Cảm ơn đã lắng nghe!*

🍜 **Food Street Guide** - Khám phá ẩm thực Vĩnh Phúc 🍜

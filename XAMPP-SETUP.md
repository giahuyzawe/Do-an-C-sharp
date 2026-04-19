# XAMPP Setup Guide - FoodStreetGuide Web Admin

## 📋 Yêu cầu

- **XAMPP** đã cài đặt (Apache + PHP)
- **Windows** với quyền Administrator

---

## 🚀 Cách 1: Symbolic Link (Khuyến nghị)

### Bước 1: Chạy Script Setup
```batch
# Mở Command Prompt với quyền Administrator
cd C:\Users\PC\FoodStreetGuide
setup-xampp.bat
```

Script sẽ tự động:
- Tạo symbolic link từ `FoodStreetGuide.Admin` → `C:\xampp\htdocs\foodstreetguide`
- Tạo thư mục `data/` và `uploads/`
- Tạo file `config.php`

### Bước 2: Khởi động Apache
1. Mở **XAMPP Control Panel**
2. Click **Start** bên cạnh Apache
3. Truy cập: http://localhost/foodstreetguide

---

## 🔧 Cách 2: Copy Files

Nếu symbolic link không hoạt động, copy thủ công:

```batch
# Xóa thư mục cũ (nếu có)
rmdir /s /q C:\xampp\htdocs\foodstreetguide

# Copy toàn bộ thư mục Admin
xcopy /s /e /i /y "C:\Users\PC\FoodStreetGuide\FoodStreetGuide.Admin" "C:\xampp\htdocs\foodstreetguide"
```

---

## 📁 Cấu trúc thư mục sau setup

```
C:\xampp\htdocs\foodstreetguide\
├── config.php          # File cấu hình
├── index.php           # Dashboard
├── login.php           # Trang đăng nhập
├── pois.php            # Quản lý POI
├── analytics-dashboard.php  # Thống kê
├── data\               # Dữ liệu JSON
│   ├── pois.json
│   ├── reviews.json
│   ├── qr_codes.json
│   ├── activities.json
│   ├── users.json
│   └── analytics.json
└── uploads\           # Ảnh upload
```

---

## ⚙️ Cấu hình File

### File `config.php` đã được tạo tự động với:

- **Auto-detect BASE_URL**: Tự động nhận diện đường dẫn
- **Data Directory**: `data/` lưu JSON files
- **Helper Functions**: `load_json()`, `save_json()`, `require_auth()`
- **Analytics Functions**: `record_analytics()`, `get_analytics_summary()`

### Các hàm helper có sẵn:

```php
<?php
require_once 'config.php';

// Load data
$pois = load_json($POIS_FILE);
$reviews = load_json($REVIEWS_FILE);

// Save data
save_json($POIS_FILE, $pois);

// Require login
require_auth(); // Chuyển hướng về login nếu chưa đăng nhập

// Record analytics
record_analytics('app_visit', ['deviceId' => 'abc123']);

// Get summary
$summary = get_analytics_summary('2024-01-01', '2024-01-31');
// Returns: ['dau' => 234, 'app_opens' => 456, 'poi_views' => 892, 'check_ins' => 156]
?>
```

---

## 🔐 Tài khoản mặc định

Sau khi setup, tạo tài khoản admin đầu tiên bằng cách tạo file `data/users.json`:

```json
[
  {
    "id": "usr_20240101123456",
    "username": "admin",
    "password": "$2y$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi",
    "name": "Administrator",
    "role": "SuperAdmin",
    "createdAt": "2024-01-01 00:00:00"
  }
]
```

**Mật khẩu mặc định**: `password`

Hoặc đăng nhập lần đầu và tạo tài khoản qua giao diện.

---

## 📊 Các trang Web Admin

| URL | Chức năng |
|-----|-----------|
| `/foodstreetguide` | Dashboard chính |
| `/foodstreetguide/login.php` | Đăng nhập |
| `/foodstreetguide/analytics-dashboard.php` | Thống kê |
| `/foodstreetguide/pois.php` | Quản lý nhà hàng |
| `/foodstreetguide/restaurant-approval.php` | Duyệt nhà hàng |
| `/foodstreetguide/audio-management.php` | Quản lý audio |
| `/foodstreetguide/qr-generator.php` | Tạo QR code |
| `/foodstreetguide/reviews.php` | Quản lý đánh giá |
| `/foodstreetguide/permissions.php` | Phân quyền |

---

## 🔄 Đồng bộ dữ liệu

Khi sử dụng **Symbolic Link**, mọi thay đổi trong `C:\Users\PC\FoodStreetGuide\FoodStreetGuide.Admin` sẽ tự động cập nhật trong XAMPP.

Nếu dùng **Copy Files**, cần chạy lại script sau mỗi lần sửa code:
```batch
setup-xampp.bat
```

---

## ❌ Xử lý lỗi

### Lỗi "Access Denied"
- Chạy XAMPP Control Panel với quyền Administrator
- Kiểm tra port 80 không bị chiếm

### Lỗi "File not found"
- Kiểm tra symbolic link đã tạo chưa: `dir C:\xampp\htdocs\foodstreetguide`
- Nếu chưa có, chạy lại `setup-xampp.bat`

### Lỗi JSON
- Xóa file `data/*.json` bị lỗi
- Hệ thống sẽ tự tạo file mới

---

## ✅ Kiểm tra sau setup

1. Truy cập http://localhost/foodstreetguide
2. Đăng nhập với tài khoản admin
3. Kiểm tra các menu:
   - Dashboard hiển thị
   - Danh sách nhà hàng
   - Trang phân tích
   - Tạo QR code

---

## 📝 Lưu ý

- **Không xóa** file `config.php` sau khi setup
- **Backup** thư mục `data/` định kỳ
- **Restart Apache** khi thay đổi config

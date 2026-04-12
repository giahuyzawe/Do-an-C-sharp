# 📋 Product Requirements Document (PRD)
## FoodStreetGuide - Khám Phá Ẩm Thực Đường Phố Quận 1

**Đề tài:** Khám Phá Ẩm Thực Đường Phố  
**Version:** 1.0  
**Date:** April 2025  

**Sinh viên thực hiện:**
- Nguyễn Gia Huy (MSSV: 3123411119)
- Vũ Gia Huy (MSSV: 3123411125)

---

## 1. TỔNG QUAN SẢN PHẨM

### 1.1 Mô tả
FoodStreetGuide là ứng dụng di động hướng dẫn du lịch ẩm thực đường phố chuyên sâu tại Quận 1, TP.HCM. Ứng dụng cung cấp trải nghiệm khám phá ẩm thực độc đáo thông qua:

- **Bản đồ tương tác**: Hiển thị các quán ăn đường phố nổi tiếng với thông tin chi tiết
- **Thuyết minh tự động**: Khi người dùng đến gần địa điểm, app tự động phát audio giới thiệu về món ăn, lịch sử quán, cách thưởng thức
- **Hệ thống geofence thông minh**: Ưu tiên các quán nổi tiếng, tránh chồng chéo thông báo
- **Đánh giá & chia sẻ**: Cộng đồng đánh giá chất lượng, giá cả, vệ sinh
- **Hỗ trợ đa ngôn ngữ**: Tiếng Việt và Tiếng Anh cho du khách quốc tế

### 1.2 Đối tượng sử dụng

#### 👨‍💼 Du khách quốc tế (Primary)
- Muốn khám phá ẩm thực đường phố Việt Nam chính thống
- Cần thông tin bằng tiếng Anh, hướng dẫn rõ ràng
- Quan tâm đến lịch sử, văn hóa từng món ăn
- Cần biết giá cả, giờ mở cửa, đánh giá từ người dùng

#### 🧑‍🤝‍🧑 Người dân địa phương (Secondary)
- Tìm quán ăn ngon, đúng chuẩn trong khu vực
- Khám phá quán mới, món lạ
- Chia sẻ trải nghiệm, đánh giá chất lượng

#### 👨‍🎓 Sinh viên, người lao động (Tertiary)
- Tìm quán ăn sáng, trưa, tối giá rẻ
- Cần thông tin nhanh, chính xác về giờ mở cửa

### 1.3 Nền tảng
- **Mobile App:** .NET MAUI (iOS, Android)
- **Web Admin:** PHP + MySQL
- **Database:** SQLite (local) + MySQL (remote)

### 1.4 Danh mục Ẩm thực Đường phố

| Danh mục | Mô tả | Ví dụ điển hình |
|----------|-------|-----------------|
| **🥖 Bánh Mì** | Bánh mì Sài Gòn đặc trưng với đa dạng nhân | Bánh Mì Huỳnh Hoa, Bánh Mì Hòa Mã |
| **🍜 Phở** | Phở bò, phở gà truyền thống | Phở Lệ, Phở Hòa Pasteur |
| **🍲 Bún** | Bún bò Huế, bún riêu, bún thịt nướng | Bún Bò Huế Đông Ba |
| **🍚 Cơm** | Cơm tấm sườn nướng - đặc sản Sài Gòn | Cơm Tấm Ba Ghiền |
| **☕ Cafe** | Cafe vợt, cafe sữa đá, cafe hiện đại | The Coffee House, Cafe vợt Nguyễn Du |
| **🍡 Vỉa hè** | Các món ăn vặt: gỏi cuốn, bánh tráng, chè | Chợ Bến Thành, Phố đi bộ Nguyễn Huệ |
| **🍺 Quán nhậu** | Ốc, lẩu, nướng đêm | Vỉa hè Phạm Văn Đồng |

### 1.5 Phạm vi triển khai
- **Khu vực tập trung:** Quận 1 (trung tâm TP.HCM)
- **Số lượng POI ban đầu:** 20+ quán ăn đường phố
- **Bán kính hoạt động:** Trong phạm vi Quận 1
- **Mở rộng:** Có thể mở rộng sang Quận 3, Quận 4, Quận 5

---

## 2. CHỨC NĂNG CHÍNH

### 2.1 Bản đồ & Định vị (Map Module)

#### 🗺️ Tính năng bản đồ

| Feature | Mô tả | Priority |
|---------|-------|----------|
| Hiển thị bản đồ | Google Maps với marker các quán ăn | P0 |
| Vị trí người dùng | GPS tracking real-time | P0 |
| Tìm kiếm | Tìm quán theo tên, món ăn, danh mục | P1 |
| Filter danh mục | Lọc theo: Bánh Mì, Phở, Bún, Cơm, Cafe... | P1 |
| Filter theo giá | Lọc theo khoảng giá: <50k, 50-100k, >100k | P2 |
| Filter theo giờ | Lọc quán đang mở cửa | P2 |
| Chỉ đường | Mở Google Maps để chỉ đường đến quán | P1 |

#### 📍 Marker & POI Card

**Marker trên bản đồ:**
- Màu sắc theo danh mục (Bánh Mì = vàng, Phở = xanh, Bún = cam...)
- Kích thước theo độ nổi tiếng (Priority 3 > 2 > 1)
- Hiển thị khoảng cách từ vị trí hiện tại

**POI Card khi tap marker:**
```
┌─────────────────────────────┐
│  [Ảnh quán]                 │
│  ⭐ 4.5 (128 đánh giá)      │
│  Bánh Mì Huỳnh Hoa 🥖       │
│  📍 26 Lê Thị Riêng          │
│  💰 ~65,000đ/ổ              │
│  🕐 06:00 - 22:00           │
│  📏 Cách bạn 250m           │
│                             │
│  [🎧] [🧭] [ℹ️] [❤️]        │
└─────────────────────────────┘
```

### 2.2 Geofence & Thuyết minh tự động

#### 🎯 Tính năng Geofence

| Feature | Mô tả | Priority |
|---------|-------|----------|
| Geofence trigger | Tự động phát audio khi vào vùng quán | P0 |
| Priority system | Quán nổi tiếng (P3) ưu tiên hơn quán thường (P1) | P0 |
| Cooldown | 5 phút giữa các lần trigger cùng quán | P0 |
| Debounce | 10m buffer để tránh trigger liên tục | P1 |
| Background tracking | Theo dõi khi app ở background | P1 |
| Smart trigger | Chỉ trigger khi người dùng đang di chuyển đến quán | P2 |

#### 🎧 Nội dung Audio Thuyết minh

**Mỗi quán có 2 dạng audio:**
1. **Audio đầy đủ** (30-60s): Lịch sử, đặc trưng, cách thưởng thức
2. **Audio ngắn** (10-15s): Tên quán + món nổi bật

**Cấu trúc nội dung thuyết minh:**
```
🎙️ Ví dụ - Bánh Mì Huỳnh Hoa:

[Tiếng Việt]
"Bạn đang đến gần Bánh Mì Huỳnh Hoa, 
 một trong những tiệm bánh mì nổi tiếng nhất Sài Gòn 
 từ năm 1989. Đặc sản ở đây là bánh mì thịt nguội 
 với pate, chả lụa, thịt heo quay. 
 Giá khoảng 65 ngàn đồng một ổ. 
 Nên thưởng thức ngay khi mua để cảm nhận độ giòn của bánh."

[English]
"You are approaching Huynh Hoa Banh Mi, 
 one of Saigon's most famous banh mi shops 
 since 1989. Their specialty is the mixed cold cut 
 banh mi with pate, Vietnamese ham, and roasted pork. 
 Price is around 65,000 VND per sandwich. 
 Best enjoyed immediately while the bread is still crispy."
```

### 2.3 Quản lý POI (Danh sách quán ăn)

#### 📋 Tính năng quản lý

| Feature | Mô tả | Priority |
|---------|-------|----------|
| Xem danh sách | Grid/list view với filter danh mục | P0 |
| Chi tiết POI | Thông tin đầy đủ về quán ăn | P0 |
| Lưu POI | Lưu quán yêu thích vào danh sách riêng | P1 |
| Thêm POI | Tạo quán mới từ Web Admin | P2 |

#### 📄 Thông tin chi tiết POI (Trang chi tiết quán)

**Header Section:**
- Ảnh bìa quán (carousel nếu có nhiều ảnh)
- Tên quán + Rating ⭐ (1-5 sao)
- Danh mục (Bánh Mì, Phở, Bún...) + Icon
- Badge: "Đang mở cửa" / "Đã đóng cửa"

**Thông tin cơ bản:**
```
📍 Địa chỉ: [Chi tiết địa chỉ]
🕐 Giờ mở cửa: [Thứ 2-CN: 06:00 - 22:00]
💰 Giá trung bình: ~[50k-100k]/người
📏 Cách bạn: [X] mét - [Y] phút đi bộ
📞 Số điện thoại: [SĐT] (nếu có)
```

**Mô tả quán:**
- Lịch sử hình thành
- Đặc trưng nổi bật
- Món ngon nên thử
- Mẹo thưởng thức

**Thông tin thêm:**
- 🅿️ Chỗ đậu xe
- ♿️ Phù hợp người khuyết tật
- 🐶 Cho phép thú cưng
- 💳 Chấp nhận thẻ tín dụng

### 2.4 Đánh giá & Review (Đánh giá quán ăn)

#### ⭐ Tiêu chí đánh giá

| Tiêu chí | Mô tả | Trọng số |
|----------|-------|----------|
| Chất lượng món ăn | Hương vị, độ tươi, cách trình bày | 40% |
| Giá cả | Giá có phù hợp chất lượng không | 20% |
| Vệ sinh | Sạch sẽ khu vực ăn uống, nhà bếp | 20% |
| Phục vụ | Thái độ nhân viên, tốc độ phục vụ | 10% |
| Không gian | Chỗ ngồi, thoáng mát, yên tĩnh | 10% |

**Rating tổng:** Trung bình có trọng số 5 tiêu chí ⭐ (1-5 sao)

#### 📝 Nội dung Review

**Review từ người dùng:**
```
┌─────────────────────────────────────┐
│ 👤 Nguyễn Văn A          ⭐⭐⭐⭐⭐  │
│ 📅 15/03/2025                      │
│                                    │
│ "Bánh mì đúng chuẩn Sài Gòn!       │
│  Pate béo, thịt nguội đầy đặn.     │
│  Giá 65k hơi cao nhưng xứng đáng.  │
│  Nên đến trước 8h sáng để tránh    │
│  xếp hàng lâu."                    │
│                                    │
│ [📷] [📷] [📷]  3 ảnh              │
│                                    │
│ 👍 24    💬 5 Trả lời              │
└─────────────────────────────────────┘
```

**Tiêu chí hữu ích:**
- 👍 Helpful (Đánh giá hữu ích)
- 🏷️ Tags: "Ngon", "Giá cao", "Xếp hàng", "Vệ sinh tốt"
- 📸 Cho phép upload tối đa 5 ảnh/review

### 2.5 Đa ngôn ngữ (Localization)
| Feature | Mô tả | Priority |
|---------|-------|----------|
| Tiếng Việt | Giao diện + audio | P0 |
| Tiếng Anh | Giao diện + audio | P0 |
| Chuyển đổi | Cài đặt ngôn ngữ trong app | P0 |

### 2.6 Đồng bộ dữ liệu
| Feature | Mô tả | Priority |
|---------|-------|----------|
| Sync từ Web | Tải POI mới từ Web Admin | P0 |
| Sync lên Web | Đẩy thay đổi local lên Web | P2 |
| Auto-sync | Tự động sync khi có mạng | P1 |
| Offline mode | Hoạt động không cần mạng | P0 |

### 2.7 Cài đặt
| Feature | Mô tả | Priority |
|---------|-------|----------|
| GPS Settings | Tần suất cập nhật vị trí | P1 |
| Geofence Settings | Bật/tắt, bán kính tối thiểu | P1 |
| API Settings | URL Web Admin, API Key | P1 |
| Audio Settings | Tốc độ đọc, âm lượng | P2 |

### 2.8 Khám phá (Discover)

#### 🔍 Tính năng khám phá ẩm thực

| Feature | Mô tả | Priority |
|---------|-------|----------|
| Danh sách quán | Grid/list view hiển thị tất cả quán | P0 |
| Tìm kiếm nâng cao | Tìm theo tên, món ăn, khu vực | P1 |
| Sắp xếp | Theo: Khoảng cách, Rating, Lượt xem, Giá | P1 |
| Lọc nâng cao | Kết hợp nhiều tiêu chí lọc | P2 |
| Gợi ý quán | "Quán gần đây", "Quán nổi tiếng" | P2 |
| Tour ẩm thực | Gợi ý lộ trình 3-5 quán | P3 |

#### 🏆 Danh mục nổi bật (Featured Categories)

**Trang Discover hiển thị:**
```
┌────────────────────────────────────────┐
│ 🔥 Quán đang nổi bật                   │
│    [Carousel 5 quán hot nhất tuần]    │
│                                        │
│ 📍 Gần bạn nhất                        │
│    [List 3 quán trong phạm vi 500m]   │
│                                        │
│ 🏷️ Danh mục phổ biến                   │
│    [🥖 Bánh Mì] [🍜 Phở] [🍲 Bún]     │
│    [🍚 Cơm]   [☕ Cafe] [🍡 Vỉa hè]   │
│                                        │
│ ⭐ Top đánh giá cao nhất               │
│    [List 5 quán 4.5+ sao]              │
│                                        │
│ 💰 Giá rẻ dưới 50k                     │
│    [List quán bình dân]                │
└────────────────────────────────────────┘
```

#### 🗺️ Tour ẩm thực đường phố (Food Tour)

**Các lộ trình gợi ý:**

| Tour | Mô tả | Số quán | Thời gian |
|------|-------|---------|-----------|
| **🌅 Tour Sáng** | Ăn sáng Sài Gòn: Bánh mì + Phở + Cafe | 3 | 2-3 giờ |
| **🌞 Tour Trưa** | Bún, Cơm tấm, Nước giải khát | 3-4 | 2-3 giờ |
| **🌙 Tour Tối** | Quán nhậu, Ốc, Lẩu đêm | 3-4 | 3-4 giờ |
| **🏛️ Tour Quận 1** | Các quán iconic nhất Quận 1 | 5 | 4-5 giờ |

---

## 3. KIẾN TRÚC HỆ THỐNG

### 3.1 Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    FoodStreetGuide App                      │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   MainPage   │  │DiscoverPage  │  │  SavedPage   │       │
│  │   (Map)      │  │(POI List)    │  │(Favorites)   │       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
│         └──────────────────┼───────────────────┘              │
│                            │                                │
│  ┌─────────────────────────┴─────────────────────────┐        │
│  │              Services Layer                      │        │
│  ├──────────────┬──────────────┬──────────────────┤        │
│  │GeofenceEngine│LocationSv    │  WebAdminSv      │        │
│  │   (Trigger)  │   (GPS)      │   (Sync)         │        │
│  ├──────────────┼──────────────┼──────────────────┤        │
│  │TTSService    │AudioPlayer   │LocalizationSv    │        │
│  │  (Narration) │   (Play)     │  (Language)       │        │
│  └──────────────┴──────────────┴──────────────────┘        │
│                            │                                │
│  ┌─────────────────────────┴─────────────────────────┐        │
│  │              Data Layer                          │        │
│  ├──────────────┬──────────────┬──────────────────┤        │
│  │  DatabaseSv  │   POI Model  │  Review Model    │        │
│  │  (SQLite)    │   (Entity)   │   (Entity)       │        │
│  └──────────────┴──────────────┴──────────────────┘        │
│                            │                                │
│  ┌─────────────────────────┴─────────────────────────┐        │
│  │              Platform Layer (Android/iOS)        │        │
│  └───────────────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    Web Admin (PHP/MySQL)                    │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   POI API    │  │  Review API  │  │  Admin UI    │       │
│  │  (REST)      │  │   (REST)     │  │  (Web)       │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Data Model

#### POI (Point of Interest / Quán ăn đường phố)

```csharp
class POI {
    // ===== Thông tin cơ bản =====
    int Id                    // Primary Key
    string NameVi             // Tên tiếng Việt
    string NameEn             // Tên tiếng Anh
    string DescriptionVi      // Mô tả chi tiết tiếng Việt
    string DescriptionEn      // Mô tả chi tiết tiếng Anh
    string ShortDescVi        // Mô tả ngắn (1-2 dòng)
    string ShortDescEn        // Short description
    
    // ===== Phân loại =====
    string Category           // Danh mục: BanhMi, Pho, Bun, Com, Cafe, ViaHe...
    string SubCategory        // Phân loại phụ: Nướng, Chay, Gốc Hoa...
    string Tags               // Tags: Ngon, Rẻ, Nổi tiếng, Xếp hàng...
    string Status             // active, inactive, maintenance, closed
    int Priority              // Độ ưu tiên: 1-3 (3 = quán nổi tiếng nhất)
    
    // ===== Vị trí & Geofence =====
    double Latitude           // Vĩ độ
    double Longitude          // Kinh độ
    double Radius             // Bán kính geofence (m) - 50m, 100m, 300m
    string Address            // Địa chỉ đầy đủ
    string District           // Quận: Quận 1, Quận 3...
    string MapUrl             // Google Maps URL
    
    // ===== Thời gian & Giá =====
    string OpeningHours       // Giờ mở cửa: "06:00-22:00"
    string DaysOpen           // Các ngày mở: "T2-CN"
    int? PriceRangeMin        // Giá thấp nhất (VNĐ)
    int? PriceRangeMax        // Giá cao nhất (VNĐ)
    string PriceDisplay       // Hiển thị: "~65k", "35k-95k"
    
    // ===== Media =====
    string Image              // URL ảnh chính
    string Images             // URLs ảnh phụ (phân cách dấu phẩy)
    string AudioVi            // URL audio thuyết minh tiếng Việt
    string AudioEn            // URL audio thuyết minh tiếng Anh
    string AudioShortVi       // Audio ngắn 10-15s
    string AudioShortEn       // Short audio
    
    // ===== Thông tin bổ sung =====
    string Phone              // Số điện thoại
    string Website            // Website/Facebook
    int? EstablishedYear      // Năm thành lập: 1989
    string Specialties        // Món đặc sản: "Bánh mì thịt nguội, Pate"
    string History            // Lịch sử quán
    string Tips               // Mẹo thưởng thức
    
    // ===== Tiện ích (Amenities) =====
    bool HasParking           // Có chỗ đậu xe
    bool HasIndoorSeating     // Có chỗ ngồi trong nhà
    bool HasOutdoorSeating    // Có chỗ ngồi vỉa hè
    bool IsWheelchairAccessible // Phù hợp xe lăn
    bool AcceptsCreditCard    // Chấp nhận thẻ
    bool IsPetFriendly        // Cho phép thú cưng
    bool HasWifi              // Có wifi
    
    // ===== Thống kê =====
    int VisitCount            // Số lượt check-in
    double RatingAverage      // Rating trung bình 1-5
    int RatingCount           // Số lượt đánh giá
    int ReviewCount           // Số bài review
    
    // ===== Sync =====
    DateTime? LastSyncFromWeb // Thời điểm sync cuối
    DateTime? CreatedAt       // Ngày tạo
    DateTime? UpdatedAt       // Ngày cập nhật
}
```

**Danh sách Category hỗ trợ:**
| Category | NameVi | Icon |
|----------|--------|------|
| BanhMi | Bánh Mì | 🥖 |
| Pho | Phở | 🍜 |
| Bun | Bún | 🍲 |
| Com | Cơm | 🍚 |
| Cafe | Cafe | ☕ |
| ViaHe | Vỉa Hè | 🍡 |
| QuanNhau | Quán Nhậu | 🍺 |
| Other | Khác | 📍 |

#### Review
```csharp
class Review {
    int Id                    // Primary Key
    int POIId                 // Foreign Key -> POI
    string UserId             // ID người dùng
    string UserName           // Tên người dùng
    string UserAvatar         // URL avatar
    int Rating                // 1-5 sao
    string Comment            // Nội dung đánh giá
    string Images             // URLs ảnh, phân cách bằng dấu phẩy
    string Status             // approved, pending, rejected
    DateTime CreatedAt        // Thời điểm tạo
    DateTime? UpdatedAt       // Thời điểm cập nhật
}
```

---

## 4. API SPECIFICATION

### 4.1 Web Admin API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/pois.php` | Get all POIs |
| GET | `/api/pois.php?id={id}` | Get POI by ID |
| POST | `/api/pois.php` | Create new POI |
| PUT | `/api/pois.php` | Update POI |
| DELETE | `/api/pois.php?id={id}` | Delete POI |
| GET | `/api/reviews.php` | Get all reviews |
| POST | `/api/reviews.php` | Add review |

### 4.2 Authentication
```
Header: Authorization: Bearer foodstreet_mobile_2024
Query: ?api_key=foodstreet_mobile_2024
```

---

## 5. UI/UX REQUIREMENTS (Yêu cầu giao diện người dùng)

### 5.1 Tab Navigation (Bottom Navigation)

| Tab | Icon | Label (VN) | Label (EN) | Mô tả |
|-----|------|------------|------------|-------|
| 🗺️ Bản đồ | map | Bản đồ | Map | Map view với marker quán ăn |
| 🔍 Khám phá | search | Khám phá | Discover | Danh sách, tìm kiếm, tour ẩm thực |
| ❤️ Đã lưu | heart | Đã lưu | Saved | Quán yêu thích đã lưu |
| ⚙️ Cài đặt | settings | Cài đặt | Settings | Ngôn ngữ, GPS, Audio |

### 5.2 POI Card UI (Thẻ thông tin quán)

#### 📱 Card trên Map (Bottom Sheet)
```
┌─────────────────────────────────────────────┐
│  ━━━ (Handle kéo lên/xuống)                │
│                                             │
│  ┌─────────────┐                            │
│  │             │  🥖 Bánh Mì Huỳnh Hoa     │
│  │   [Ảnh    │  ⭐⭐⭐⭐⭐ (128)            │
│  │    quán]   │  📍 26 Lê Thị Riêng, Q.1   │
│  │             │  ⏱️ Cách bạn 250m (3 phút) │
│  └─────────────┘                            │
│                                             │
│  💰 ~65,000đ    🕐 06:00-22:00   🟢 Mở cửa │
│                                             │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                             │
│  [🎧 Nghe]  [🧭 Đường]  [ℹ️ Chi tiết] [❤️] │
└─────────────────────────────────────────────┘
```

#### 📋 Card trong List (Discover Page)
```
┌─────────────────────────────────────────────┐
│ ┌──────────┐  🥖 Bánh Mì Huỳnh Hoa        │
│ │         │  ⭐⭐⭐⭐⭐ (128 đánh giá)      │
│ │  [Ảnh]  │  📍 Quận 1  |  🥖 Bánh Mì     │
│ │         │  ⏱️ 250m  |  💰 ~65k         │
│ └──────────┘                               │
└─────────────────────────────────────────────┘
```

#### 🔘 Action Buttons (Nút hành động)

| Nút | Icon | Chức năng | Khi nhấn |
|-----|------|-----------|----------|
| Nghe | 🎧 | Nghe thuyết minh | Phát audio giới thiệu quán |
| Chỉ đường | 🧭 | Mở Google Maps | Chuyển sang Maps để chỉ đường |
| Chi tiết | ℹ️ | Xem chi tiết | Mở trang chi tiết POI |
| Lưu | ❤️ | Thêm vào yêu thích | Lưu vào danh sách cá nhân |
| Chia sẻ | ↗️ | Share quán | Mở dialog chia sẻ |

### 5.3 Color Palette (Bảng màu)

| Mục đích | Mã màu | Tên |
|----------|--------|-----|
| Primary (Chính) | #FF6B35 | Orange - Màu ẩm thực, nhiệt huyết |
| Secondary (Phụ) | #2EC4B6 | Teal - Màu tươi mát |
| Background (Nền) | #FFFFFF | White |
| Surface (Bề mặt) | #F8F9FA | Light Gray |
| Text Primary | #212529 | Dark Gray |
| Text Secondary | #6C757D | Medium Gray |
| Success (Thành công) | #28A745 | Green |
| Warning (Cảnh báo) | #FFC107 | Yellow |
| Error (Lỗi) | #DC3545 | Red |
| Geofence Active | #4CAF50 | Green Circle |
| Geofence Inactive | #9E9E9E | Gray Circle |

### 5.4 Typography (Kiểu chữ)

| Element | Font | Size | Weight |
|---------|------|------|--------|
| App Title | Roboto | 24sp | Bold |
| POI Name | Roboto | 18sp | SemiBold |
| Section Header | Roboto | 16sp | SemiBold |
| Body Text | Roboto | 14sp | Regular |
| Caption | Roboto | 12sp | Regular |
| Price | Roboto | 16sp | Bold |

### 5.5 Iconography (Hệ thống icon)

| Danh mục | Icon | Màu marker |
|----------|------|------------|
| Bánh Mì | 🥖 | Vàng #FFD54F |
| Phở | 🍜 | Xanh lá #81C784 |
| Bún | 🍲 | Cam #FF8A65 |
| Cơm | � | Nâu #A1887F |
| Cafe | ☕ | Nâu đậm #6D4C41 |
| Vỉa hè | 🍡 | Hồng #F06292 |
| Quán nhậu | 🍺 | Amber #FFB74D |

### 5.6 Responsive Design

**Mobile (Primary):**
- Portrait mode ưu tiên
- Touch target tối thiểu 48dp
- Swipe gestures cho navigation

**Tablet (Secondary):**
- Split view: Map (60%) | List (40%)
- Landscape mode hỗ trợ tốt hơn

---

## 6. NON-FUNCTIONAL REQUIREMENTS

### 6.1 Performance
| Metric | Target |
|--------|--------|
| App launch | < 3 giây |
| Map render | < 2 giây |
| POI list load | < 1 giây |
| Geofence response | < 500ms |

### 6.2 Security
- API Key authentication
- SQLite encryption (optional)
- HTTPS for API communication

### 6.3 Reliability
- Offline mode support
- Auto-retry failed sync
- Data backup/restore

---

## 7. MILESTONE & TIMELINE

| Phase | Thời gian | Deliverable |
|-------|-----------|-------------|
| MVP | Month 1 | Map, Geofence, Basic POI |
| v1.0 | Month 2 | Reviews, Sync, Localization |
| v1.1 | Month 3 | Audio TTS, QR Scan, Polish |

---

## 8. APPENDIX

### 8.1 Danh sách Quán ăn Đường phố Mẫu (Quận 1)

#### 🥖 Danh mục Bánh Mì
| Tên quán | Lat | Lng | Priority | Giá | Giờ mở cửa |
|----------|-----|-----|----------|-----|------------|
| Bánh Mì Huỳnh Hoa | 10.7701 | 106.6923 | 3 | ~65k | 06:00-22:00 |
| Bánh Mì Hòa Mã | 10.7692 | 106.6865 | 2 | ~35k | 06:00-21:00 |
| Bánh Mì Nguyên Sinh | 10.7715 | 106.6954 | 2 | ~40k | 07:00-20:00 |

#### 🍜 Danh mục Phở
| Tên quán | Lat | Lng | Priority | Giá | Giờ mở cửa |
|----------|-----|-----|----------|-----|------------|
| Phở Lệ | 10.7545 | 106.6677 | 3 | ~75k | 06:00-22:00 |
| Phở Hòa Pasteur | 10.7798 | 106.6928 | 3 | ~65k | 05:00-21:00 |
| Phở Bò Vân | 10.7732 | 106.6932 | 2 | ~55k | 06:00-20:00 |

#### 🍲 Danh mục Bún
| Tên quán | Lat | Lng | Priority | Giá | Giờ mở cửa |
|----------|-----|-----|----------|-----|------------|
| Bún Bò Huế Đông Ba | 10.7760 | 106.6955 | 3 | ~60k | 06:30-21:30 |
| Bún Thịt Nướng Cô Giang | 10.7731 | 106.6915 | 2 | ~45k | 10:00-21:00 |
| Bún Riêu Gánh | 10.7741 | 106.6940 | 2 | ~40k | 06:00-12:00 |

#### 🍚 Danh mục Cơm
| Tên quán | Lat | Lng | Priority | Giá | Giờ mở cửa |
|----------|-----|-----|----------|-----|------------|
| Cơm Tấm Cali | 10.7752 | 106.6885 | 3 | ~50k | 07:00-23:00 |
| Cơm Tấm 27 | 10.7708 | 106.6912 | 2 | ~35k | 08:00-22:00 |

#### ☕ Danh mục Cafe
| Tên quán | Lat | Lng | Priority | Giá | Giờ mở cửa |
|----------|-----|-----|----------|-----|------------|
| The Coffee House | 10.7769 | 106.7009 | 3 | ~45k | 07:00-22:00 |
| Cafe vợt Nguyễn Du | 10.7755 | 106.6938 | 2 | ~25k | 06:00-18:00 |
| Trung Nguyên Legend | 10.7750 | 106.7050 | 2 | ~55k | 07:00-23:00 |

#### 🍡 Danh mục Vỉa hè / Chợ
| Tên quán | Lat | Lng | Priority | Giá | Giờ mở cửa |
|----------|-----|-----|----------|-----|------------|
| Chợ Bến Thành (Khu ăn uống) | 10.7720 | 106.6983 | 3 | ~50k | 06:00-18:00 |
| Hẻm 42 Tôn Thất Thiệp | 10.7745 | 106.7012 | 2 | ~40k | 16:00-23:00 |
| Phố đi bộ Nguyễn Huệ | 10.7765 | 106.7019 | 2 | ~60k | 18:00-24:00 |

### 8.2 Sample Audio Scripts (Mẫu kịch bản Audio)

#### 🥖 Bánh Mì Huỳnh Hoa (30 giây)
**[VN]** "Bạn đang đến gần Bánh Mì Huỳnh Hoa, tiệm bánh mì nổi tiếng nhất Sài Gòn từ năm 1989. Đặc sản ở đây là bánh mì thịt nguội đầy đặn với pate béo, chả lụa, thịt heo quay giòn, patê gan, và bơ. Một ổ bánh mì đầy đủ giá 65 ngàn đồng. Nên thưởng thức ngay khi mua để cảm nhận độ giòn rụm của bánh mì nóng hổi."

**[EN]** "You are approaching Huynh Hoa Banh Mi, Saigon's most famous banh mi shop since 1989. Their specialty is the fully-loaded mixed cold cut banh mi with rich pate, Vietnamese ham, crispy roasted pork, liver pate, and butter. A complete sandwich costs 65,000 VND. Best enjoyed immediately while the bread is still hot and crispy."

#### 🍜 Phở Lệ (30 giây)
**[VN]** "Bạn đang đến gần Phở Lệ, quán phở nổi tiếng 50 năm tuổi ở Sài Gòn. Phở ở đây nổi tiếng với nước dùng đậm đà, ngọt thanh tự nhiên từ xương ống hầm kỹ. Đặc biệt là món phở tái chín với thịt bò mềm, tái tái vừa chín tới. Giá từ 75 đến 95 ngàn đồng một tô tùy loại. Quán mở từ 6 giờ sáng đến 10 giờ tối."

**[EN]** "You are approaching Pho Le, a legendary 50-year-old pho restaurant in Saigon. Famous for its rich yet naturally sweet broth, simmered carefully from beef bones. Their specialty is the tai chin pho with tender rare and well-done beef. Prices range from 75,000 to 95,000 VND per bowl. Open from 6 AM to 10 PM."

### 8.3 Glossary (Thuật ngữ)

| Thuật ngữ | Tiếng Anh | Định nghĩa |
|-----------|-----------|-----------|
| **POI** | Point of Interest | Điểm đến/quan tâm (quán ăn, địa điểm) |
| **Geofence** | Geographic Fence | Vùng địa lý ảo để trigger sự kiện khi người dùng vào/ra |
| **TTS** | Text-to-Speech | Công nghệ chuyển văn bản thành giọng nói |
| **Cooldown** | Cooldown Period | Thời gian chờ giữa các lần trigger liên tiếp |
| **Debounce** | Debounce Buffer | Khoảng đệm để tránh trigger liên tục do GPS nhảy |
| **Radius** | Radius | Bán kính geofence tính bằng mét |
| **Priority** | Priority Level | Mức độ ưu tiên: 3 (cao) > 2 (trung) > 1 (thấp) |
| **Sync** | Synchronization | Đồng bộ dữ liệu giữa app và server |
| **Offline Mode** | Offline Mode | Chế độ hoạt động không cần kết nối mạng |

# 🍜 Food Tour System - UML Diagrams

## 📋 Danh sách chức năng và sơ đồ

| # | Chức năng | Use Case | Activity | Sequence |
|---|-----------|----------|----------|----------|
| 1 | Người dùng mở app xem bản đồ | ✅ | ✅ | ✅ |
| 2 | Người dùng vào vùng POI (Geofence) | ✅ | ✅ | ✅ |
| 3 | Người dùng xem chi tiết POI | ✅ | ✅ | ✅ |
| 4 | Người dùng check-in bằng QR | ✅ | ✅ | ✅ |
| 5 | Người dùng đánh giá POI | ✅ | ✅ | ✅ |
| 6 | Admin quản lý nhà hàng (POI) | ✅ | ✅ | ✅ |
| 7 | Admin/Owner tạo QR code | ✅ | ✅ | ✅ |
| 8 | Admin/Owner xem thống kê | ✅ | ✅ | ✅ |
| 9 | Admin quản lý đánh giá | ✅ | ✅ | ✅ |
| 10 | Đồng bộ dữ liệu App ↔ Web Admin | ✅ | ✅ | ✅ |

---

## 1️⃣ USE CASE DIAGRAMS

### UC-01: Tổng quan hệ thống

```plantuml
@startuml
left to right direction
skinparam packageStyle rectangle

actor "Người dùng" as User
actor "Khách hàng" as Customer
actor "Chủ nhà hàng" as Owner
actor "Admin" as Admin

rectangle "Food Tour System" {
    
    package "Mobile App" {
        usecase "Xem bản đồ POI" as UC_ViewMap
        usecase "Xem chi tiết nhà hàng" as UC_ViewDetail
        usecase "Check-in QR" as UC_Checkin
        usecase "Đánh giá nhà hàng" as UC_Review
        usecase "Nghe thuyết minh" as UC_Listen
        usecase "Chỉ đường" as UC_Directions
        usecase "Lưu nhà hàng" as UC_Save
    }
    
    package "Web Admin" {
        usecase "Quản lý nhà hàng" as UC_ManagePOI
        usecase "Tạo QR Code" as UC_CreateQR
        usecase "Xem thống kê" as UC_Stats
        usecase "Quản lý đánh giá" as UC_ManageReview
        usecase "Phê duyệt nhà hàng" as UC_Approve
        usecase "Quản lý người dùng" as UC_ManageUser
    }
    
    package "System" {
        usecase "Đồng bộ dữ liệu" as UC_Sync
        usecase "Geofencing" as UC_Geofence
        usecase "TTS Narration" as UC_TTS
    }
}

' User relationships
User --> UC_ViewMap
User --> UC_ViewDetail
User --> UC_Checkin
User --> UC_Review
User --> UC_Listen
User --> UC_Directions
User --> UC_Save

' Owner relationships
Owner --> UC_ManagePOI
Owner --> UC_CreateQR
Owner --> UC_Stats
Owner --> UC_ManageReview

' Admin relationships
Admin --> UC_ManagePOI
Admin --> UC_CreateQR
Admin --> UC_Stats
Admin --> UC_ManageReview
Admin --> UC_Approve
Admin --> UC_ManageUser

' System
UC_Geofence ..> UC_ViewDetail : <<include>>
UC_Sync ..> UC_Review : <<include>>

@enduml
```

---

### UC-02: Mobile App - Xem bản đồ và POI

```plantuml
@startuml
left to right direction

actor "Người dùng" as User

rectangle "Mobile App - Map View" {
    usecase "Mở ứng dụng" as UC_Open
    usecase "Hiển thị bản đồ" as UC_ShowMap
    usecase "Tải POI từ server" as UC_LoadPOI
    usecase "Hiển thị marker POI" as UC_ShowMarkers
    usecase "Theo dõi vị trí" as UC_Tracking
    usecase "Phát hiện POI gần nhất" as UC_DetectNearest
    usecase "Hiển thị bottom sheet" as UC_ShowSheet
}

User --> UC_Open
UC_Open --> UC_ShowMap
UC_ShowMap --> UC_LoadPOI
UC_LoadPOI --> UC_ShowMarkers
UC_ShowMap --> UC_Tracking
UC_Tracking --> UC_DetectNearest
UC_DetectNearest --> UC_ShowSheet

@enduml
```

---

### UC-03: Mobile App - Check-in QR

```plantuml
@startuml
left to right direction

actor "Người dùng" as User
actor "Web Admin" as Web

rectangle "QR Check-in System" {
    usecase "Quét mã QR" as UC_Scan
    usecase "Mở ứng dụng từ deep link" as UC_DeepLink
    usecase "Xác thực mã QR" as UC_Validate
    usecase "Ghi nhận check-in" as UC_Record
    usecase "Hiển thị thông tin POI" as UC_ShowInfo
    usecase "Cập nhật thống kê" as UC_UpdateStats
}

User --> UC_Scan
User --> UC_DeepLink
UC_Scan --> UC_Validate
UC_DeepLink --> UC_Validate
UC_Validate --> UC_Record : Valid
UC_Validate --> UC_ShowInfo : Valid
UC_Record --> UC_UpdateStats
Web --> UC_UpdateStats

@enduml
```

---

### UC-04: Mobile App - Đánh giá nhà hàng

```plantuml
@startuml
left to right direction

actor "Người dùng" as User

rectangle "Review System" {
    usecase "Xem đánh giá hiện có" as UC_ViewReviews
    usecase "Mở form đánh giá" as UC_OpenReview
    usecase "Chọn số sao" as UC_Rate
    usecase "Viết bình luận" as UC_Comment
    usecase "Gửi đánh giá" as UC_Submit
    usecase "Lưu local (SQLite)" as UC_SaveLocal
    usecase "Đồng bộ Web Admin" as UC_SyncWeb
    usecase "Cập nhật rating POI" as UC_UpdateRating
}

User --> UC_ViewReviews
User --> UC_OpenReview
UC_OpenReview --> UC_Rate
UC_Rate --> UC_Comment
UC_Comment --> UC_Submit
UC_Submit --> UC_SaveLocal
UC_SaveLocal --> UC_SyncWeb
UC_SyncWeb --> UC_UpdateRating

@enduml
```

---

### UC-05: Web Admin - Quản lý nhà hàng

```plantuml
@startuml
left to right direction

actor "Admin" as Admin
actor "Chủ nhà hàng" as Owner

rectangle "Web Admin - POI Management" {
    usecase "Đăng nhập" as UC_Login
    usecase "Xem danh sách nhà hàng" as UC_ListPOI
    usecase "Thêm nhà hàng mới" as UC_AddPOI
    usecase "Chỉnh sửa thông tin" as UC_EditPOI
    usecase "Xóa nhà hàng" as UC_DeletePOI
    usecase "Upload hình ảnh" as UC_UploadImage
    usecase "Phê duyệt nhà hàng" as UC_ApprovePOI
    
    package "Content" {
        usecase "Quản lý mô tả" as UC_ManageDesc
        usecase "Quản lý giờ mở cửa" as UC_ManageHours
        usecase "Quản lý điện thoại" as UC_ManagePhone
        usecase "Quản lý ảnh" as UC_ManageImages
    }
}

Admin --> UC_Login
Admin --> UC_ListPOI
Admin --> UC_AddPOI
Admin --> UC_EditPOI
Admin --> UC_DeletePOI
Admin --> UC_ApprovePOI

Owner --> UC_Login
Owner --> UC_ListPOI
Owner --> UC_EditPOI
Owner --> UC_UploadImage

UC_AddPOI --> UC_ManageDesc
UC_AddPOI --> UC_ManageHours
UC_AddPOI --> UC_ManagePhone
UC_AddPOI --> UC_ManageImages

@enduml
```

---

### UC-06: Web Admin - Quản lý QR Code

```plantuml
@startuml
left to right direction

actor "Admin/Owner" as User

rectangle "QR Code Management" {
    usecase "Chọn nhà hàng" as UC_SelectPOI
    usecase "Tạo QR Code mới" as UC_GenerateQR
    usecase "Thiết lập thời hạn" as UC_SetExpiry
    usecase "Thiết lập giới hạn quét" as UC_SetMaxScan
    usecase "Lưu QR vào hệ thống" as UC_SaveQR
    usecase "Tải về/ in QR" as UC_DownloadQR
    usecase "Xem lịch sử quét" as UC_ViewScanHistory
    usecase "Vô hiệu hóa QR" as UC_DisableQR
}

User --> UC_SelectPOI
UC_SelectPOI --> UC_GenerateQR
UC_GenerateQR --> UC_SetExpiry
UC_GenerateQR --> UC_SetMaxScan
UC_SetExpiry --> UC_SaveQR
UC_SetMaxScan --> UC_SaveQR
UC_SaveQR --> UC_DownloadQR
UC_SaveQR --> UC_ViewScanHistory
User --> UC_DisableQR

@enduml
```

---

### UC-07: Web Admin - Thống kê & Analytics

```plantuml
@startuml
left to right direction

actor "Admin" as Admin
actor "Chủ nhà hàng" as Owner

rectangle "Statistics Dashboard" {
    usecase "Xem tổng quan" as UC_Dashboard
    
    package "Metrics" {
        usecase "Số người dùng (DAU)" as UC_DAU
        usecase "Lượt xem POI" as UC_Views
        usecase "Lượt check-in" as UC_Checkins
        usecase "Số đánh giá" as UC_ReviewCount
        usecase "Rating trung bình" as UC_AvgRating
    }
    
    package "Time Range" {
        usecase "Thống kê hôm nay" as UC_Today
        usecase "Thống kê 7 ngày" as UC_Week
        usecase "Thống kê 30 ngày" as UC_Month
    }
    
    package "Filter" {
        usecase "Lọc theo nhà hàng" as UC_FilterPOI
        usecase "So sánh nhà hàng" as UC_Compare
    }
}

Admin --> UC_Dashboard
Admin --> UC_DAU
Admin --> UC_Views
Admin --> UC_Checkins
Admin --> UC_ReviewCount
Admin --> UC_AvgRating

Owner --> UC_Dashboard
Owner --> UC_Views : My POIs only
Owner --> UC_Checkins : My POIs only
Owner --> UC_ReviewCount : My POIs only

UC_Dashboard --> UC_Today
UC_Dashboard --> UC_Week
UC_Dashboard --> UC_Month

@enduml
```

---

## 2️⃣ ACTIVITY DIAGRAMS

### ACT-01: Người dùng mở app và xem bản đồ

```plantuml
@startuml
start

:Người dùng mở ứng dụng;

fork
    :Tải POI từ Web Admin API;
    :Lưu POI vào SQLite;
fork again
    :Khởi tạo bản đồ (Google Maps);
    :Hiển thị vị trí hiện tại;
fork again
    :Bắt đầu theo dõi vị trí
    (Location Service);
    :Thiết lập Geofence cho các POI;
end fork

:Hiển thị marker các POI trên bản đồ;

while (Người dùng di chuyển?) is (Có)
    :Cập nhật vị trí;
    :Tính khoảng cách đến các POI;
    if (Vào vùng Geofence?) then (Có)
        :Hiển thị Bottom Sheet
        thông tin POI;
        :Phát thuyết minh TTS;
    else (Không)
        if (Ra khỏi vùng?) then (Có)
            :Ẩn Bottom Sheet;
        endif
    endif
endwhile (Dừng)

:Dừng theo dõi;

stop
@enduml
```

---

### ACT-02: Người dùng check-in bằng QR Code

```plantuml
@startuml
start

:Người dùng quét QR Code
tại nhà hàng;

if (Đã cài app?) then (Chưa)
    :Chuyển đến trang tải APK;
    :Hướng dẫn cài đặt;
    stop
else (Rồi)
endif

:Mở ứng dụng qua Deep Link
foodtour://qr/{token};

:Gửi request đến
/api/check-qr.php;

if (Mã QR hợp lệ?) then (Có)
    if (Hết hạn?) then (Có)
        :Hiển thị "Mã QR đã hết hạn";
        stop
    else (Không)
        if (Vượt giới hạn quét?) then (Có)
            :Hiển thị "Đã hết lượt quét";
            stop
        else (Không)
            if (Đã check-in 1h qua?) then (Có)
                :Hiển thị "Đã check-in rồi";
                stop
            else (Không)
            endif
        endif
    endif
else (Không)
    :Hiển thị "Mã QR không hợp lệ";
    stop
endif

:Ghi nhận check-in thành công;
:Cập nhật scanCount trong QR code;
:Tăng checkInCount của POI;
:Gửi analytics "check_in";

:Hiển thị thông tin POI;
:Mở Bottom Sheet chi tiết;

:🎉 "Check-in thành công!";

stop
@enduml
```

---

### ACT-03: Người dùng đánh giá nhà hàng

```plantuml
@startuml
start

:Người dùng xem chi tiết POI;

if (Muốn đánh giá?) then (Không)
    :Xem đánh giá hiện có;
    :Kết thúc;
    stop
else (Có)
endif

:Click "Thêm đánh giá";
:Hiển thị form đánh giá;

:Chọn số sao (1-5);

if (Viết bình luận?) then (Có)
    :Nhập nội dung;
else (Không)
endif

:Gửi đánh giá;

fork
    :Lưu vào SQLite (local);
fork again
    :Hiển thị ngay trên UI;
fork again
    :Gửi đến Web Admin API
    /api/post-review.php;
end fork

:Cập nhật rating trung bình
 của POI trên Web Admin;

:🎉 "Cảm ơn đánh giá của bạn!";

:Đóng form;
:Reload danh sách đánh giá;

stop
@enduml
```

---

### ACT-04: Admin quản lý nhà hàng (POI)

```plantuml
@startuml
start

:Admin đăng nhập Web Admin;

if (Role = Admin?) then (Có)
    :Xem tất cả nhà hàng;
else (Owner)
    :Chỉ xem nhà hàng của mình;
endif

switch (Hành động?) 
case (Thêm mới)
    :Nhập thông tin nhà hàng;
    :Tên (VI/EN);
    :Địa chỉ;
    :Tọa độ (Lat/Long);
    :Mô tả;
    :Giờ mở cửa;
    :Điện thoại;
    :Upload hình ảnh;
    
    if (Role = Admin?) then (Có)
        :Tự động phê duyệt;
    else (Owner)
        :Chờ Admin phê duyệt;
    endif
    
    :Lưu vào database;
    :Đồng bộ đến Mobile App;
    
case (Chỉnh sửa)
    :Chọn nhà hàng;
    :Cập nhật thông tin;
    :Lưu thay đổi;
    :Cập nhật App;
    
case (Xóa)
    :Chọn nhà hàng;
    if (Xác nhận xóa?) then (Có)
        :Xóa khỏi database;
        :Xóa khỏi App cache;
    endif
    
case (Phê duyệt)
    :Xem danh sách chờ;
    :Review thông tin;
    if (Hợp lệ?) then (Có)
        :Chuyển status = approved;
        :Thông báo cho Owner;
    else (Không)
        :Chuyển status = rejected;
        :Ghi lý do;
    endif
endswitch

:Cập nhật thành công;

stop
@enduml
```

---

### ACT-05: Tạo và quản lý QR Code

```plantuml
@startuml
start

:Admin/Owner đăng nhập;
:Chọn chức năng QR Code;

if (Tạo mới?) then (Có)
    :Chọn nhà hàng;
    :Cấu hình QR Code;
    
    :Thiết lập thời hạn;
    note right
      - Không thời hạn
      - 1 ngày
      - 7 ngày
      - 30 ngày
      - Tùy chỉnh
    end note
    
    :Thiết lập giới hạn quét;
    note right
      - Không giới hạn
      - 100 lượt
      - 500 lượt
      - Tùy chỉnh
    end note
    
    :Tạo mã token duy nhất;
    :Lưu QR Code vào database;
    :Generate QR image;
    :Hiển thị QR Code;
    
    if (Tải về?) then (Có)
        :Download PNG/SVG;
    endif
    
else (Xem quản lý)
    :Xem danh sách QR Code;
    
    switch (Hành động)
    case (Xem lịch sử)
        :Hiển thị danh sách quét;
        :Thời gian, Device ID, Số thứ tự;
    case (Vô hiệu hóa)
        :Set isActive = false;
        :QR không còn hiệu lực;
    case (Gia hạn)
        :Cập nhật ngày hết hạn mới;
        :Tăng giới hạn quét;
    endswitch
endif

:Cập nhật thành công;

stop
@enduml
```

---

### ACT-06: Đồng bộ dữ liệu App ↔ Web Admin

```plantuml
@startuml
start

partition "Mobile App" {
    :App khởi động;
    
    fork
        :Đồng bộ POI;
        :Gọi /api/get-pois.php;
        :So sánh với local;
        :Cập nhật/Thêm/Xóa;
        :Lưu SQLite;
    fork again
        :Đồng bộ Reviews;
        if (Có review mới chưa sync?) then (Có)
            :Gửi /api/post-review.php;
            :Nhận ID từ Web;
            :Cập nhật WebReviewId;
        endif
        :Fetch reviews từ Web;
        :Merge với local;
    fork again
        :Gửi Analytics;
        :app_visit, poi_view, check_in;
    end fork
}

partition "Web Admin" {
    :API nhận request;
    
    switch (Request type)
    case (get-pois)
        :Query POI database;
        :Trả về JSON;
    case (post-review)
        :Lưu review mới;
        :Tính lại avg rating;
        :Trả về success + ID;
    case (check-qr)
        :Validate QR token;
        :Update scanCount;
        :Tăng POI checkInCount;
        :Return POI info;
    case (post-analytics)
        :Lưu analytics;
        :Cập nhật thống kê;
    endswitch
}

:Đồng bộ hoàn tất;

stop
@enduml
```

---

## 3️⃣ SEQUENCE DIAGRAMS

### SEQ-01: Người dùng vào vùng POI (Geofence Trigger)

```plantuml
@startuml
actor "Người dùng" as User
participant "Mobile App" as App
participant "LocationService" as Loc
participant "GeofenceEngine" as Geo
participant "DatabaseService" as DB
participant "TTSService" as TTS

User -> App: Mở ứng dụng
activate App

App -> Loc: StartTracking()
activate Loc
Loc --> App: LocationUpdated event

App -> Geo: SetPOIs(poiList)
activate Geo
Geo --> App: Geofence enabled

User -> Loc: Di chuyển đến gần POI
Loc -> Geo: OnLocationChanged(lat, long)

Geo -> Geo: Check distance < radius
Geo -> App: POIEntered event

App -> DB: GetPOIAsync(poiId)
activate DB
DB --> App: POI details

App -> App: ShowPOICard()
App -> App: CollapseBottomSheet()
App -> App: Populate fields

App -> TTS: SpeakAsync(narration)
activate TTS
TTS --> App: Speech completed

App -> App: SendAnalytics("poi_entered")

App --> User: Hiển thị Bottom Sheet

deactivate App
deactivate Loc
deactivate Geo
deactivate DB
deactivate TTS

@enduml
```

---

### SEQ-02: Check-in bằng QR Code

```plantuml
@startuml
actor "Người dùng" as User
actor "Web Browser" as Browser
participant "Mobile App" as App
participant "MainActivity" as Main
participant "ApiService" as API
participant "Web Admin\nqr-redirect.php" as WebQR
participant "Web Admin\ncheck-qr.php" as CheckQR
participant "Database\n(JSON files)" as DB

User -> Browser: Quét QR Code
activate Browser

Browser -> WebQR: GET ?token=abc123
activate WebQR

WebQR -> DB: Load QR codes
DB --> WebQR: QR data

WebQR -> WebQR: Validate token

alt App chưa cài
    WebQR --> Browser: Trang tải APK
    User -> Browser: Tải & cài đặt
else App đã cài
    WebQR --> Browser: Redirect foodtour://qr/abc123
    Browser -> App: Open deep link
    
    activate App
    App -> Main: OnCreate(intent)
    activate Main
    Main -> Main: Extract token
    Main -> App: Set DeepLinkToken
    Main --> App: MainPage()
    
    App -> API: CheckQRAsync(token, deviceId)
    activate API
    API -> CheckQR: POST {token, deviceId}
    
    activate CheckQR
    CheckQR -> DB: Load QR codes
    DB --> CheckQR: QR list
    
    CheckQR -> CheckQR: Validate QR
    CheckQR -> CheckQR: Check expiry
    CheckQR -> CheckQR: Check maxScans
    CheckQR -> CheckQR: Check cooldown
    
    alt QR hợp lệ
        CheckQR -> DB: Update scanCount
        CheckQR -> DB: Add scan record
        CheckQR -> DB: Update POI checkInCount
        DB --> CheckQR: Success
        
        CheckQR --> API: {success: true, poiId: 1, ...}
        API --> App: POI data
        
        App -> API: PostAnalytics("check_in")
        API --> App: Success
        
        App -> App: Navigate to POI Detail
        App --> User: 🎉 Check-in thành công!
        
    else QR không hợp lệ
        CheckQR --> API: {success: false, error: "..."}
        API --> App: Error
        App --> User: Hiển thị lỗi
    end
    
    deactivate CheckQR
    deactivate API
    deactivate Main
endif

deactivate WebQR
deactivate Browser
deactivate App

@enduml
```

---

### SEQ-03: Người dùng đánh giá nhà hàng

```plantuml
@startuml
actor "Người dùng" as User
participant "Mobile App" as App
participant "MainPage" as Main
participant "DatabaseService" as DB
participant "ApiService" as API
participant "Web Admin\nget-reviews.php" as GetReviews
participant "Web Admin\npost-review.php" as PostReview
participant "SQLite" as SQLite

User -> App: Vuốt lên Bottom Sheet
activate App

App -> Main: ExpandBottomSheet()
activate Main
Main -> Main: LoadReviewsForPOI(poiId)

Main -> DB: GetReviewsAsync(poiId)
activate DB
DB -> SQLite: SELECT * FROM Reviews
SQLite --> DB: Local reviews
DB --> Main: reviews list

Main -> API: GetReviewsAsync(poiId)
activate API
API -> GetReviews: GET ?poiId=1
activate GetReviews
GetReviews -> GetReviews: Load reviews.json
GetReviews --> API: {reviews: [...]}
deactivate GetReviews
API --> Main: Web reviews

loop Merge reviews
    alt Review mới từ Web
        Main -> DB: AddReviewAsync(review)
        DB -> SQLite: INSERT
    else Review bị xóa trên Web
        Main -> DB: DeleteReviewByWebId(id)
        DB -> SQLite: DELETE
    end
end

Main -> Main: Render reviews UI
Main --> App: Hiển thị đánh giá

User -> App: Click "Thêm đánh giá"
App -> Main: OnAddReviewClicked()
Main --> User: Hiện form đánh giá

User -> App: Chọn 5 sao + Viết comment
App -> Main: OnSubmitReviewClicked()

Main -> Main: Validate (rating > 0)

Main -> DB: AddReviewAsync(review)
DB -> SQLite: INSERT
SQLite --> DB: Success
DB --> Main: Review saved

Main -> Main: Hide form
Main -> Main: Reload reviews UI

Main --> User: Hiển thị đánh giá mới

Main -> API: PostReviewAsync(review)
activate API
API -> PostReview: POST review data
activate PostReview
PostReview -> PostReview: Save to reviews.json
PostReview -> PostReview: Recalculate POI rating
PostReview --> API: {success: true, id: "rev_123"}
deactivate PostReview
API --> Main: Sync success

Main -> Main: Log "Review synced"
deactivate API

User <-- App: 🎉 "Cảm ơn đánh giá!"

deactivate Main
deactivate App

deactivate DB

@enduml
```

---

### SEQ-04: Admin thêm nhà hàng mới

```plantuml
@startuml
actor "Admin" as Admin
participant "Web Admin\nPHP Pages" as Web
participant "pois.php" as POIs
participant "API\npoi-api.php" as API
participant "File System\n(data/*.json)" as FS

Admin -> Web: Đăng nhập
activate Web
Web --> Admin: Dashboard

Admin -> POIs: Click "Thêm nhà hàng"
activate POIs
POIs --> Admin: Form thêm mới

Admin -> POIs: Nhập thông tin
note right
  - NameVI, NameEN
  - Address, Lat, Long
  - Description
  - OpeningHours
  - Phone
  - ImageUrl
end note

POIs -> API: POST /poi-api.php?action=create
activate API

API -> API: Validate input
API -> FS: Load pois.json
activate FS
FS --> API: Existing POIs

API -> API: Generate new ID
API -> API: Create POI object
API -> FS: Save pois.json
FS --> API: Success
deactivate FS

API --> POIs: {success: true, id: 123}
deactivate API

POIs --> Admin: "Thêm thành công!"
POIs -> POIs: Refresh danh sách

deactivate POIs

Admin <-- Web: Cập nhật danh sách POI

deactivate Web

@enduml
```

---

### SEQ-05: Tạo QR Code

```plantuml
@startuml
actor "Chủ nhà hàng" as Owner
participant "Web Admin\nqr-generator.php" as QRGen
participant "API\ngenerate-qr.php" as API
participant "File System\n(data/*.json)" as FS

Owner -> QRGen: Chọn nhà hàng + Click "Tạo QR"
activate QRGen

QRGen --> Owner: Form cấu hình

Owner -> QRGen: Thiết lập:
note right
  - Expires: 7 days
  - MaxScans: 100
  - Single use: false
end note

QRGen -> API: POST generate
activate API

API -> API: Generate unique token
API -> FS: Load qr-codes.json
activate FS
FS --> API: Existing QR codes

API -> API: Create QR object
API -> API: Set poiId, expiry, maxScans
API -> FS: Save qr-codes.json
FS --> API: Success
deactivate FS

API -> API: Generate QR image (QRcode lib)
API --> QRGen: {success: true, token, image}
deactivate API

QRGen --> Owner: Hiển thị QR Code
QRGen -> QRGen: Tạo link download

Owner -> QRGen: Click "Tải về"
QRGen --> Owner: Download PNG

deactivate QRGen

@enduml
```

---

### SEQ-06: Xem thống kê (Analytics)

```plantuml
@startuml
actor "Admin" as Admin
actor "Chủ nhà hàng" as Owner
participant "Web Admin\nstatistics.php" as Stats
participant "API\nget-analytics.php" as API
participant "Database\n(analytics.json)" as DB

alt Admin xem thống kê
    Admin -> Stats: Vào trang Statistics
    activate Stats
    
    Stats -> API: GET /get-analytics.php
    activate API
    API -> DB: Load analytics.json
    activate DB
    DB --> API: All records
    deactivate DB
    
    API -> API: Calculate metrics
    note right
      - DAU (Distinct deviceId)
      - Views (type='poi_view')
      - Check-ins (type='check_in')
      - By date range
    end note
    
    API --> Stats: {today, week, month}
    deactivate API
    
    Stats --> Admin: Dashboard thống kê
    
else Owner xem thống kê
    Owner -> Stats: Vào trang Statistics
    Stats -> API: GET ?role=owner&poiIds=[1,2]
    API -> DB: Filter by poiId
    DB --> API: Filtered records
    API --> Stats: Owner stats only
    Stats --> Owner: Thống kê nhà hàng của tôi
end

deactivate Stats

@enduml
```

---

### SEQ-07: Đồng bộ dữ liệu (App khởi động)

```plantuml
@startuml
participant "Mobile App" as App
participant "App.xaml.cs" as AppCS
participant "ApiService" as API
participant "DatabaseService" as DB
participant "SQLite" as SQLite
participant "Web Admin\nAPI" as WebAPI
participant "File System" as FS

App -> AppCS: OnStart()
activate App
activate AppCS

par Sync POIs
    AppCS -> API: GetPOIsAsync()
    activate API
    API -> WebAPI: GET /get-pois.php
    activate WebAPI
    WebAPI -> FS: Load pois.json
    FS --> WebAPI: POI data
    WebAPI --> API: {success, data}
    deactivate WebAPI
    API --> AppCS: POI list
    deactivate API
    
    AppCS -> DB: SavePOIsAsync(list)
    activate DB
    DB -> SQLite: INSERT/UPDATE
    SQLite --> DB: Success
    DB --> AppCS: Done
    deactivate DB
    
    AppCS -> AppCS: Debug.Log("POIs synced")

and Sync Reviews
    AppCS --> DB: GetPendingReviews()
    DB -> SQLite: SELECT unsynced
    SQLite --> DB: pending list
    DB --> AppCS: reviews
    
    loop Each pending review
        AppCS -> API: PostReviewAsync()
        API -> WebAPI: POST /post-review.php
        WebAPI -> FS: Save & update rating
        WebAPI --> API: {webReviewId}
        API --> AppCS: success
        AppCS -> DB: Update WebReviewId
    end
    
    AppCS -> API: GetReviewsAsync()
    API -> WebAPI: GET /get-reviews.php
    WebAPI --> API: all reviews
    API --> AppCS: web reviews
    AppCS -> DB: MergeReviews()
end

AppCS -> AppCS: Setup completed

AppCS --> App: Ready

deactivate AppCS
deactivate App

@enduml
```

---

## 📝 Tóm tắt số lượng sơ đồ

| Loại sơ đồ | Số lượng | Chi tiết |
|------------|----------|----------|
| **Use Case** | 7 | Tổng quan, Map, QR Check-in, Review, POI Management, QR Management, Statistics |
| **Activity** | 6 | Map view, QR Check-in, Review, POI CRUD, QR Management, Data Sync |
| **Sequence** | 7 | Geofence, QR Check-in, Review, Add POI, Create QR, Analytics, App Sync |
| **TỔNG** | **20** | |

---

## 🎯 Cách xem sơ đồ

1. Copy code PlantUML
2. Dán vào [www.plantuml.com/plantuml](https://www.plantuml.com/plantuml)
3. Hoặc cài extension PlantUML trong VS Code
4. Xem trực tiếp sơ đồ được render

---

*Generated for Food Tour System - Mobile App + Web Admin*

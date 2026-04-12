# FoodStreetGuide - Database Architecture

## Tổng quan kiến trúc Database thống nhất

Database PostgreSQL được thiết kế để phục vụ cả **Web Admin** và **Mobile App** sử dụng chung một database schema.

## Sơ đồ các bảng chính

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         FOODSTREETGUIDE DATABASE                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐   │
│  │   AspNetUsers   │    │   AspNetRoles   │    │  Identity Tables        │   │
│  │  (Admin Users)  │    │   (Roles)       │    │                         │   │
│  └─────────────────┘    └─────────────────┘    └─────────────────────────┘   │
│                                                                             │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐   │
│  │     POIs        │◄───│  Narrations     │    │  MediaFiles             │   │
│  │  (Locations)    │    │  (Audio/TTS)    │    │  (Images/Audio)         │   │
│  └─────────────────┘    └─────────────────┘    └─────────────────────────┘   │
│          │                       │                                          │
│          ▼                       ▼                                          │
│  ┌─────────────────┐    ┌─────────────────┐                               │
│  │   POIVisits     │    │ NarrationPlays  │  ← Analytics/Tracking            │
│  │  (User visits)  │    │  (Play count)   │                               │
│  └─────────────────┘    └─────────────────┘                               │
│                                                                             │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐   │
│  │  Restaurants    │◄───│   MenuItems     │    │  RestaurantReviews      │   │
│  │  (Food places)  │    │  (Menu/Food)    │    │  (Ratings)              │   │
│  └─────────────────┘    └─────────────────┘    └─────────────────────────┘   │
│                                                                             │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐   │
│  │   AppUsers      │◄───│  SavedPlaces    │    │  UserTrackings          │   │
│  │  (Mobile users) │    │  (Favorites)    │    │  (GPS tracking)         │   │
│  └─────────────────┘    └─────────────────┘    └─────────────────────────┘   │
│                                                                             │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐   │
│  │  GeofenceConfig │    │  AppSettings    │    │  OfflinePackages        │   │
│  │  (Geofencing)   │    │  (Config)       │    │  (Offline data)         │   │
│  └─────────────────┘    └─────────────────┘    └─────────────────────────┘   │
│                                                                             │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────────────┐   │
│  │   SystemLogs    │    │   Notifications │    │  ApiKeys                │   │
│  │  (Audit logs)   │    │  (Push notif)   │    │  (API management)       │   │
│  └─────────────────┘    └─────────────────┘    └─────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Chi tiết các bảng

### 1. Identity Tables (ASP.NET Core Identity)
- `AspNetUsers` - Admin users cho Web Admin
- `AspNetRoles` - Roles: SuperAdmin, Admin, Editor, Moderator
- `AspNetUserRoles`, `AspNetUserClaims`, etc.

### 2. POI Management
```sql
Table: POIs
- Id (PK)
- NameVi, NameEn (Tên địa điểm)
- DescriptionVi, DescriptionEn (Mô tả)
- Category (landmark, restaurant, cafe, market, etc.)
- Latitude, Longitude (Tọa độ)
- Radius (Bán kính trigger - meters)
- Priority (1=Low, 2=Medium, 3=High)
- NarrationType (tts / audio)
- ImageUrl, AudioUrl
- Tags (JSON array)
- Status (active/inactive/draft)
- VisitCount, CreatedAt, UpdatedAt

Table: Narrations
- Id, POIId (FK)
- Language (vi, en)
- Type (tts, audio)
- TextScript (TTS text)
- VoiceId, AudioUrl
- DurationSeconds, Status

Table: NarrationPlays (Analytics)
- Id, NarrationId, AppUserId
- PlayedAt, Completed
```

### 3. Restaurant Management
```sql
Table: Restaurants
- Id, Name, Description, Address
- Latitude, Longitude
- Category (street_food, restaurant, cafe, bar, night_market)
- OpenHours, Phone, Website
- PriceRange ($, $$, $$$)
- Rating, RatingCount
- Images (comma-separated URLs)
- IsHighlighted, Status

Table: MenuItems
- Id, RestaurantId (FK)
- Name, Description, Price, ImageUrl, IsPopular

Table: RestaurantReviews
- Id, RestaurantId, AppUserId
- Rating (1-5), Comment, CreatedAt
```

### 4. User Management (Mobile App)
```sql
Table: AppUsers (Mobile users)
- Id, Name, Email
- DeviceId (UNIQUE - định danh thiết bị)
- AvatarUrl, PreferredLanguage
- JoinDate, LastActive, Status

Table: SavedPlaces (Địa điểm đã lưu)
- Id, AppUserId
- POIId hoặc RestaurantId
- Type ('poi' hoặc 'restaurant')
- SavedAt

Table: UserTrackings (Lịch sử di chuyển)
- Id, AppUserId
- Latitude, Longitude, Accuracy
- Timestamp (partition theo tháng)

Table: POIVisits (Lịch sử ghé thăm)
- Id, POIId, AppUserId
- Latitude, Longitude, VisitedAt
```

### 5. Geofence & Settings
```sql
Table: GeofenceConfigs
- Id, POIId/RestaurantId
- TriggerType (enter, exit, near)
- Radius, CooldownMinutes, Priority

Table: AppSettings (Cấu hình app)
- Key, Value, Description, Category

Table: OfflinePackages (Gói dữ liệu offline)
- Id, Name, RegionName
- CenterLat, CenterLng, RadiusKm
- IncludedPOIs, IncludedRestaurants
- PackageSizeBytes, DownloadUrl, Version
```

### 6. System & Admin
```sql
Table: MediaFiles (Quản lý file upload)
- Id, FileName, FileType (image/audio)
- MimeType, FileSize, StoragePath
- PublicUrl, ThumbnailUrl
- UploadedById, UploadedAt

Table: SystemLogs
- Id, Level, Category, Message
- Details, Exception, StackTrace
- UserId, IpAddress, UserAgent, Timestamp

Table: Notifications (Push notifications)
- Id, Title, Message, ImageUrl
- TargetType (all, nearby, specific)
- TargetUsers, TargetLat, TargetLng, TargetRadiusKm
- SentCount, DeliveredCount, OpenedCount
- Status, ScheduledAt, SentAt

Table: ApiKeys
- Id, Key (UNIQUE), Name, Description
- Permissions (array), AllowedIps
- RateLimitPerMinute, UsageCount, ExpiresAt
```

## Phương pháp đồng bộ Web Admin ↔ Mobile App

### 1. Shared Database
- Cả Web Admin và Mobile App đều kết nối đến cùng một PostgreSQL database
- Mobile App gọi REST API để đọc/ghi dữ liệu
- Web Admin dùng Razor Pages + EF Core trực tiếp

### 2. API cho Mobile App
```csharp
// Mobile App sẽ gọi các endpoints:
GET /api/pois/nearby?lat={lat}&lng={lng}&radius={meters}
GET /api/pois/{id}
GET /api/pois/{id}/narration?lang={vi|en}

GET /api/restaurants/nearby?lat={lat}&lng={lng}
GET /api/restaurants/{id}
GET /api/restaurants/{id}/menu
GET /api/restaurants/{id}/reviews

POST /api/users/register (DeviceId registration)
POST /api/users/{id}/tracking (Log location)
POST /api/users/{id}/visits (Log POI visit)
POST /api/users/{id}/saved-places

GET /api/offline-packages
GET /api/offline-packages/{id}/download
```

### 3. Data Flow

```
┌──────────────────┐         ┌──────────────────┐         ┌──────────────────┐
│   Mobile App     │◄───────►│   .NET Web API   │◄───────►│   PostgreSQL     │
│  (React Native/  │  HTTP   │   (REST API)     │  EF Core│   Database       │
│   Flutter)       │         │                  │         │                  │
└──────────────────┘         └──────────────────┘         └──────────────────┘
                                      ▲
                                      │
                              ┌───────┴────────┐
                              │   Web Admin      │
                              │  (Razor Pages)   │
                              └──────────────────┘
```

### 4. Real-time Sync (Optional)
- WebSocket/Socket.io cho real-time tracking
- Firebase Cloud Messaging cho push notifications

## Cài đặt Database

### 1. Tạo Database PostgreSQL
```bash
# Install PostgreSQL 15+
createdb foodstreetguide

# Enable PostGIS (optional, for advanced spatial queries)
psql foodstreetguide -c "CREATE EXTENSION postgis;"
```

### 2. Chạy SQL Script
```bash
psql foodstreetguide < database_schema.sql
```

### 3. Cấu hình Connection String
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=foodstreetguide;Username=postgres;Password=yourpassword;Port=5432"
  }
}
```

### 4. Tạo Admin User mặc định
```sql
-- Sau khi chạy migrations, tạo admin user:
-- Password: Admin@123
INSERT INTO "AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
    "FullName", "Role", "IsActive", "CreatedAt")
VALUES (
    gen_random_uuid()::text,
    'admin@foodstreetguide.com',
    'ADMIN@FOODSTREETGUIDE.COM',
    'admin@foodstreetguide.com',
    'ADMIN@FOODSTREETGUIDE.COM',
    TRUE,
    -- Password hash cho 'Admin@123' (generate bằng ASP.NET Identity)
    'AQAAAAIAAYagAAAAEBx...',
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    FALSE, FALSE, TRUE, 0,
    'Super Admin',
    'SuperAdmin',
    TRUE,
    NOW()
);
```

## Indexes và Performance

Các index quan trọng đã được tạo trong schema:
- `POIs`: Index trên Category, Status, Priority, Location (GIST)
- `AppUsers`: Index trên DeviceId (UNIQUE)
- `POIVisits`: Index trên POIId, AppUserId, VisitedAt
- `UserTrackings`: Partition theo tháng
- `Narrations`: Index trên POIId, Language

## Backup & Restore

```bash
# Backup
pg_dump foodstreetguide > backup_$(date +%Y%m%d).sql

# Restore
psql foodstreetguide < backup_20240101.sql
```

## Security

1. **Connection Encryption**: Dùng SSL cho production
2. **API Authentication**: JWT tokens cho Mobile App
3. **Rate Limiting**: Cấu hình trong ApiKeys table
4. **Audit Logging**: Mọi thay đổi được log vào SystemLogs

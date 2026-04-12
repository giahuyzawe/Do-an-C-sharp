# FoodStreetGuide - Web Admin

Web Admin cho ứng dụng khám phá quán ăn FoodStreetGuide.

## Công nghệ

- **Backend**: .NET 9.0 Web API + Razor Pages
- **Database**: PostgreSQL 15+ với Entity Framework Core
- **Frontend**: Bootstrap 5, Bootstrap Icons, Leaflet Maps, Chart.js
- **Authentication**: ASP.NET Core Identity

## Tính năng chính

1. **Dashboard**: Thống kê tổng quan với biểu đồ và bản đồ
2. **POI Management**: Quản lý điểm đến với map picker
3. **Restaurant Management**: Quản lý quán ăn và menu
4. **User Management**: Quản lý người dùng app
5. **Content/Narration**: Quản lý audio thuyết minh
6. **Analytics**: Thống kê lượt truy cập, rating
7. **Media Library**: Quản lý ảnh và audio
8. **Settings**: Cấu hình geofence, TTS, map
9. **System**: API keys, logs, notifications

## Cấu trúc thư mục

```
/
├── Data/
│   └── ApplicationDbContext.cs    # EF Core DbContext
├── Models/                        # Entity Models
│   ├── POI.cs
│   ├── Restaurant.cs
│   ├── AppUser.cs
│   ├── Narration.cs
│   ├── AdminUser.cs
│   └── ...
├── Pages/
│   ├── Index.cshtml               # Dashboard
│   ├── POIs/                      # POI CRUD
│   ├── Restaurants/               # Restaurant CRUD
│   ├── Users/                     # User Management
│   ├── Settings/                  # App Settings
│   ├── Auth/                      # Login/Logout
│   └── Shared/_Layout.cshtml      # Master layout
├── wwwroot/
│   └── uploads/                   # Uploaded files
├── database_schema.sql            # SQL migration script
└── DATABASE_ARCHITECTURE.md       # Database documentation
```

## Cài đặt

### 1. Prerequisites
- .NET 9.0 SDK
- PostgreSQL 15+
- Visual Studio 2022 hoặc VS Code

### 2. Database Setup

```bash
# Tạo database
createdb foodstreetguide

# Chạy migration script
psql foodstreetguide < database_schema.sql
```

### 3. Cấu hình Connection String

Chỉnh sửa `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=foodstreetguide;Username=postgres;Password=yourpassword"
  }
}
```

### 4. Chạy ứng dụng

```bash
cd "d:\New folder\htdocs\webadmin"
dotnet restore
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Truy cập: `https://localhost:5001`

## Tạo Admin User

```bash
dotnet run --seed
# Hoặc tạo thủ công quan SQL xem database_schema.sql
```

## API cho Mobile App

Xem tài liệu chi tiết trong `DATABASE_ARCHITECTURE.md`.

Các endpoints chính:
- `GET /api/pois/nearby?lat={}&lng={}&radius={}`
- `GET /api/restaurants/nearby?lat={}&lng={}`
- `POST /api/users/{id}/tracking`
- `POST /api/users/{id}/visits`

## Database thống nhất

Web Admin và Mobile App dùng chung một PostgreSQL database:
- Web Admin: Razor Pages + EF Core trực tiếp
- Mobile App: Gọi REST API → Web API → Database

Xem chi tiết kiến trúc trong `DATABASE_ARCHITECTURE.md`.

## License

MIT

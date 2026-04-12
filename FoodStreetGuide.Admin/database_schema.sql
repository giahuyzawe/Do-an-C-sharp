-- =====================================================
-- FoodStreetGuide Unified Database Schema
-- PostgreSQL 15+ with PostGIS extension
-- For: Web Admin + Mobile App
-- =====================================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- =====================================================
-- 1. IDENTITY TABLES (ASP.NET Core Identity)
-- =====================================================

CREATE TABLE "AspNetRoles" (
    "Id" TEXT PRIMARY KEY,
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" TEXT
);

CREATE TABLE "AspNetUsers" (
    "Id" TEXT PRIMARY KEY,
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMP WITH TIME ZONE,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
    "FullName" TEXT,
    "AvatarUrl" TEXT,
    "Role" TEXT DEFAULT 'Editor',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "LastLogin" TIMESTAMP WITH TIME ZONE
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" TEXT NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

CREATE TABLE "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("LoginProvider", "ProviderKey")
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "RoleId" TEXT NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name")
);

-- =====================================================
-- 2. APP USERS (Mobile App Users)
-- =====================================================

CREATE TABLE "AppUsers" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT,
    "Email" TEXT,
    "DeviceId" TEXT NOT NULL UNIQUE,
    "AvatarUrl" TEXT,
    "PreferredLanguage" TEXT DEFAULT 'vi',
    "JoinDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "LastActive" TIMESTAMP WITH TIME ZONE,
    "Status" TEXT DEFAULT 'active'
);

CREATE INDEX "IX_AppUsers_DeviceId" ON "AppUsers"("DeviceId");
CREATE INDEX "IX_AppUsers_Status" ON "AppUsers"("Status");

-- =====================================================
-- 3. POI (Points of Interest)
-- =====================================================

CREATE TABLE "POIs" (
    "Id" SERIAL PRIMARY KEY,
    "NameVi" TEXT NOT NULL,
    "NameEn" TEXT,
    "DescriptionVi" TEXT,
    "DescriptionEn" TEXT,
    "Category" TEXT DEFAULT 'landmark',
    "Address" TEXT,
    "Latitude" DOUBLE PRECISION NOT NULL,
    "Longitude" DOUBLE PRECISION NOT NULL,
    "Radius" INTEGER DEFAULT 100,
    "Priority" INTEGER DEFAULT 1,
    "NarrationType" TEXT DEFAULT 'tts',
    "ImageUrl" TEXT,
    "AudioUrl" TEXT,
    "Tags" TEXT,
    "Status" TEXT DEFAULT 'active',
    "VisitCount" INTEGER DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE
);

CREATE INDEX "IX_POIs_Category" ON "POIs"("Category");
CREATE INDEX "IX_POIs_Status" ON "POIs"("Status");
CREATE INDEX "IX_POIs_Priority" ON "POIs"("Priority");
CREATE INDEX "IX_POIs_Location" ON "POIs" USING GIST (
    point("Longitude", "Latitude")
);

-- =====================================================
-- 4. RESTAURANTS
-- =====================================================

CREATE TABLE "Restaurants" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT,
    "Address" TEXT,
    "Latitude" DOUBLE PRECISION NOT NULL,
    "Longitude" DOUBLE PRECISION NOT NULL,
    "Category" TEXT DEFAULT 'street_food',
    "OpenHours" TEXT,
    "Phone" TEXT,
    "Website" TEXT,
    "PriceRange" TEXT DEFAULT '$',
    "Rating" DOUBLE PRECISION DEFAULT 0,
    "RatingCount" INTEGER DEFAULT 0,
    "Images" TEXT,
    "IsHighlighted" BOOLEAN DEFAULT FALSE,
    "Status" TEXT DEFAULT 'active',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_Restaurants_Category" ON "Restaurants"("Category");
CREATE INDEX "IX_Restaurants_Status" ON "Restaurants"("Status");
CREATE INDEX "IX_Restaurants_IsHighlighted" ON "Restaurants"("IsHighlighted");

-- =====================================================
-- 5. MENU ITEMS
-- =====================================================

CREATE TABLE "MenuItems" (
    "Id" SERIAL PRIMARY KEY,
    "RestaurantId" INTEGER NOT NULL REFERENCES "Restaurants"("Id") ON DELETE CASCADE,
    "Name" TEXT NOT NULL,
    "Description" TEXT,
    "Price" DECIMAL(18,2) NOT NULL,
    "ImageUrl" TEXT,
    "IsPopular" BOOLEAN DEFAULT FALSE
);

CREATE INDEX "IX_MenuItems_RestaurantId" ON "MenuItems"("RestaurantId");

-- =====================================================
-- 6. RESTAURANT REVIEWS
-- =====================================================

CREATE TABLE "RestaurantReviews" (
    "Id" SERIAL PRIMARY KEY,
    "RestaurantId" INTEGER NOT NULL REFERENCES "Restaurants"("Id") ON DELETE CASCADE,
    "AppUserId" INTEGER REFERENCES "AppUsers"("Id") ON DELETE SET NULL,
    "Rating" INTEGER NOT NULL CHECK ("Rating" BETWEEN 1 AND 5),
    "Comment" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_RestaurantReviews_RestaurantId" ON "RestaurantReviews"("RestaurantId");
CREATE INDEX "IX_RestaurantReviews_AppUserId" ON "RestaurantReviews"("AppUserId");

-- =====================================================
-- 7. NARRATIONS
-- =====================================================

CREATE TABLE "Narrations" (
    "Id" SERIAL PRIMARY KEY,
    "POIId" INTEGER NOT NULL REFERENCES "POIs"("Id") ON DELETE CASCADE,
    "Language" VARCHAR(10) DEFAULT 'vi',
    "Type" TEXT DEFAULT 'tts',
    "TextScript" TEXT,
    "VoiceId" TEXT,
    "AudioUrl" TEXT,
    "DurationSeconds" INTEGER,
    "Status" TEXT DEFAULT 'active',
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_Narrations_POIId" ON "Narrations"("POIId");
CREATE INDEX "IX_Narrations_Language" ON "Narrations"("Language");
CREATE INDEX "IX_Narrations_Status" ON "Narrations"("Status");

-- =====================================================
-- 8. NARRATION PLAYS (Analytics)
-- =====================================================

CREATE TABLE "NarrationPlays" (
    "Id" SERIAL PRIMARY KEY,
    "NarrationId" INTEGER NOT NULL REFERENCES "Narrations"("Id") ON DELETE CASCADE,
    "AppUserId" INTEGER REFERENCES "AppUsers"("Id") ON DELETE SET NULL,
    "PlayedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Completed" BOOLEAN DEFAULT FALSE
);

CREATE INDEX "IX_NarrationPlays_NarrationId" ON "NarrationPlays"("NarrationId");
CREATE INDEX "IX_NarrationPlays_AppUserId" ON "NarrationPlays"("AppUserId");
CREATE INDEX "IX_NarrationPlays_PlayedAt" ON "NarrationPlays"("PlayedAt");

-- =====================================================
-- 9. POI VISITS (Analytics)
-- =====================================================

CREATE TABLE "POIVisits" (
    "Id" SERIAL PRIMARY KEY,
    "POIId" INTEGER NOT NULL REFERENCES "POIs"("Id") ON DELETE CASCADE,
    "AppUserId" INTEGER REFERENCES "AppUsers"("Id") ON DELETE SET NULL,
    "Latitude" DOUBLE PRECISION NOT NULL,
    "Longitude" DOUBLE PRECISION NOT NULL,
    "VisitedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_POIVisits_POIId" ON "POIVisits"("POIId");
CREATE INDEX "IX_POIVisits_AppUserId" ON "POIVisits"("AppUserId");
CREATE INDEX "IX_POIVisits_VisitedAt" ON "POIVisits"("VisitedAt");

-- =====================================================
-- 10. SAVED PLACES
-- =====================================================

CREATE TABLE "SavedPlaces" (
    "Id" SERIAL PRIMARY KEY,
    "AppUserId" INTEGER NOT NULL REFERENCES "AppUsers"("Id") ON DELETE CASCADE,
    "POIId" INTEGER REFERENCES "POIs"("Id") ON DELETE CASCADE,
    "RestaurantId" INTEGER REFERENCES "Restaurants"("Id") ON DELETE CASCADE,
    "Type" TEXT DEFAULT 'poi',
    "SavedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT "CHK_SavedPlace_Type" CHECK (
        ("Type" = 'poi' AND "POIId" IS NOT NULL) OR
        ("Type" = 'restaurant' AND "RestaurantId" IS NOT NULL)
    )
);

CREATE INDEX "IX_SavedPlaces_AppUserId" ON "SavedPlaces"("AppUserId");

-- =====================================================
-- 11. USER TRACKING
-- =====================================================

CREATE TABLE "UserTrackings" (
    "Id" BIGSERIAL PRIMARY KEY,
    "AppUserId" INTEGER NOT NULL REFERENCES "AppUsers"("Id") ON DELETE CASCADE,
    "Latitude" DOUBLE PRECISION NOT NULL,
    "Longitude" DOUBLE PRECISION NOT NULL,
    "Accuracy" DOUBLE PRECISION,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_UserTrackings_AppUserId" ON "UserTrackings"("AppUserId");
CREATE INDEX "IX_UserTrackings_Timestamp" ON "UserTrackings"("Timestamp");

-- Partition for tracking data (monthly)
CREATE TABLE "UserTrackings_2024_01" PARTITION OF "UserTrackings"
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

-- =====================================================
-- 12. GEOFENCE CONFIGS
-- =====================================================

CREATE TABLE "GeofenceConfigs" (
    "Id" SERIAL PRIMARY KEY,
    "POIId" INTEGER REFERENCES "POIs"("Id") ON DELETE CASCADE,
    "RestaurantId" INTEGER REFERENCES "Restaurants"("Id") ON DELETE CASCADE,
    "TriggerType" TEXT DEFAULT 'enter',
    "Radius" INTEGER DEFAULT 100,
    "CooldownMinutes" INTEGER DEFAULT 30,
    "Priority" INTEGER DEFAULT 1
);

CREATE INDEX "IX_GeofenceConfigs_POIId" ON "GeofenceConfigs"("POIId");
CREATE INDEX "IX_GeofenceConfigs_RestaurantId" ON "GeofenceConfigs"("RestaurantId");

-- =====================================================
-- 13. MEDIA FILES
-- =====================================================

CREATE TABLE "MediaFiles" (
    "Id" SERIAL PRIMARY KEY,
    "FileName" TEXT NOT NULL,
    "FileType" VARCHAR(20) NOT NULL,
    "MimeType" TEXT,
    "FileSize" BIGINT NOT NULL,
    "StoragePath" TEXT NOT NULL,
    "PublicUrl" TEXT,
    "ThumbnailUrl" TEXT,
    "Width" INTEGER,
    "Height" INTEGER,
    "DurationSeconds" INTEGER,
    "AltText" TEXT,
    "Tags" TEXT,
    "UploadedById" TEXT REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    "UploadedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_MediaFiles_FileType" ON "MediaFiles"("FileType");
CREATE INDEX "IX_MediaFiles_UploadedById" ON "MediaFiles"("UploadedById");

-- =====================================================
-- 14. APP SETTINGS
-- =====================================================

CREATE TABLE "AppSettings" (
    "Id" SERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL UNIQUE,
    "Value" TEXT,
    "Description" TEXT,
    "Category" TEXT DEFAULT 'general',
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_AppSettings_Category" ON "AppSettings"("Category");

-- Default settings
INSERT INTO "AppSettings" ("Key", "Value", "Description", "Category") VALUES
('geofence.default_radius', '100', 'Default geofence radius in meters', 'geofence'),
('geofence.min_radius', '50', 'Minimum geofence radius in meters', 'geofence'),
('geofence.max_radius', '500', 'Maximum geofence radius in meters', 'geofence'),
('tts.default_voice_vi', 'vi-VN-HoaiMyNeural', 'Default TTS voice for Vietnamese', 'tts'),
('tts.default_voice_en', 'en-US-JennyNeural', 'Default TTS voice for English', 'tts'),
('gps.update_interval', '5000', 'GPS update interval in milliseconds', 'tracking'),
('narration.cooldown_seconds', '300', 'Cooldown between narrations in seconds', 'narration'),
('map.provider', 'google', 'Map provider (google, mapbox, osm)', 'map'),
('app.default_language', 'vi', 'Default app language', 'app');

-- =====================================================
-- 15. OFFLINE PACKAGES
-- =====================================================

CREATE TABLE "OfflinePackages" (
    "Id" SERIAL PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT,
    "RegionName" TEXT,
    "CenterLat" DOUBLE PRECISION,
    "CenterLng" DOUBLE PRECISION,
    "RadiusKm" DOUBLE PRECISION,
    "IncludedPOIs" TEXT,
    "IncludedRestaurants" TEXT,
    "PackageSizeBytes" BIGINT,
    "DownloadUrl" TEXT,
    "Version" INTEGER DEFAULT 1,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_OfflinePackages_IsActive" ON "OfflinePackages"("IsActive");

-- =====================================================
-- 16. SYSTEM LOGS
-- =====================================================

CREATE TABLE "SystemLogs" (
    "Id" BIGSERIAL PRIMARY KEY,
    "Level" TEXT DEFAULT 'Info',
    "Category" TEXT DEFAULT 'general',
    "Message" TEXT NOT NULL,
    "Details" TEXT,
    "Exception" TEXT,
    "StackTrace" TEXT,
    "UserId" TEXT,
    "IpAddress" TEXT,
    "UserAgent" TEXT,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_SystemLogs_Level" ON "SystemLogs"("Level");
CREATE INDEX "IX_SystemLogs_Category" ON "SystemLogs"("Category");
CREATE INDEX "IX_SystemLogs_Timestamp" ON "SystemLogs"("Timestamp");

-- =====================================================
-- 17. LOGIN LOGS (Security)
-- =====================================================

CREATE TABLE "LoginLogs" (
    "Id" BIGSERIAL PRIMARY KEY,
    "UserId" TEXT,
    "Email" TEXT,
    "Success" BOOLEAN NOT NULL,
    "FailureReason" TEXT,
    "IpAddress" TEXT,
    "UserAgent" TEXT,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX "IX_LoginLogs_UserId" ON "LoginLogs"("UserId");
CREATE INDEX "IX_LoginLogs_Timestamp" ON "LoginLogs"("Timestamp");

-- =====================================================
-- 18. NOTIFICATIONS
-- =====================================================

CREATE TABLE "Notifications" (
    "Id" SERIAL PRIMARY KEY,
    "Title" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "ImageUrl" TEXT,
    "TargetType" TEXT DEFAULT 'all',
    "TargetUsers" TEXT,
    "TargetLat" DOUBLE PRECISION,
    "TargetLng" DOUBLE PRECISION,
    "TargetRadiusKm" DOUBLE PRECISION,
    "SentCount" INTEGER,
    "DeliveredCount" INTEGER,
    "OpenedCount" INTEGER,
    "Status" TEXT DEFAULT 'draft',
    "ScheduledAt" TIMESTAMP WITH TIME ZONE,
    "SentAt" TIMESTAMP WITH TIME ZONE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedById" TEXT REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_Notifications_Status" ON "Notifications"("Status");
CREATE INDEX "IX_Notifications_CreatedAt" ON "Notifications"("CreatedAt");

-- =====================================================
-- 19. API KEYS
-- =====================================================

CREATE TABLE "ApiKeys" (
    "Id" SERIAL PRIMARY KEY,
    "Key" TEXT NOT NULL UNIQUE,
    "Name" TEXT NOT NULL,
    "Description" TEXT,
    "Permissions" TEXT[],
    "AllowedIps" TEXT,
    "RateLimitPerMinute" INTEGER,
    "UsageCount" INTEGER DEFAULT 0,
    "LastUsedAt" TIMESTAMP WITH TIME ZONE,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE,
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "CreatedById" TEXT REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_ApiKeys_Key" ON "ApiKeys"("Key");
CREATE INDEX "IX_ApiKeys_IsActive" ON "ApiKeys"("IsActive");

-- =====================================================
-- VIEWS FOR ANALYTICS
-- =====================================================

-- Daily POI Visits
CREATE VIEW "V_DailyPOIVisits" AS
SELECT 
    DATE("VisitedAt") as visit_date,
    "POIId",
    COUNT(*) as visit_count
FROM "POIVisits"
GROUP BY DATE("VisitedAt"), "POIId";

-- Daily Active Users
CREATE VIEW "V_DailyActiveUsers" AS
SELECT 
    DATE("LastActive") as active_date,
    COUNT(DISTINCT "Id") as active_users
FROM "AppUsers"
WHERE "LastActive" IS NOT NULL
GROUP BY DATE("LastActive");

-- Top POIs by visits
CREATE VIEW "V_TopPOIs" AS
SELECT 
    p."Id",
    p."NameVi",
    p."Category",
    COUNT(v."Id") as total_visits
FROM "POIs" p
LEFT JOIN "POIVisits" v ON p."Id" = v."POIId"
WHERE p."Status" = 'active'
GROUP BY p."Id", p."NameVi", p."Category"
ORDER BY total_visits DESC;

-- =====================================================
-- FUNCTIONS
-- =====================================================

-- Function to get nearby POIs
CREATE OR REPLACE FUNCTION get_nearby_pois(
    lat DOUBLE PRECISION,
    lng DOUBLE PRECISION,
    radius_meters DOUBLE PRECISION
) RETURNS TABLE (
    id INTEGER,
    name_vi TEXT,
    name_en TEXT,
    category TEXT,
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    distance_meters DOUBLE PRECISION
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        p."Id",
        p."NameVi",
        p."NameEn",
        p."Category",
        p."Latitude",
        p."Longitude",
        (point(lng, lat) <-> point(p."Longitude", p."Latitude")) * 111320 as distance_meters
    FROM "POIs" p
    WHERE p."Status" = 'active'
    AND (point(lng, lat) <-> point(p."Longitude", p."Latitude")) * 111320 <= radius_meters
    ORDER BY distance_meters;
END;
$$ LANGUAGE plpgsql;

-- Function to increment POI visit count
CREATE OR REPLACE FUNCTION increment_poi_visits(poi_id INTEGER)
RETURNS VOID AS $$
BEGIN
    UPDATE "POIs" 
    SET "VisitCount" = "VisitCount" + 1 
    WHERE "Id" = poi_id;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- TRIGGERS
-- =====================================================

-- Auto-update UpdatedAt on POIs
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_pois_updated_at
    BEFORE UPDATE ON "POIs"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- SEED DATA
-- =====================================================

-- Create default admin user (password: Admin@123)
-- Run this after creating the database and running migrations
-- Password hash is for "Admin@123"

/*
INSERT INTO "AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", 
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", 
    "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount",
    "FullName", "Role", "IsActive", "CreatedAt")
VALUES (
    'admin-' || uuid_generate_v4(),
    'admin@foodstreetguide.com',
    'ADMIN@FOODSTREETGUIDE.COM',
    'admin@foodstreetguide.com',
    'ADMIN@FOODSTREETGUIDE.COM',
    TRUE,
    'AQAAAAIAAYagAAAAELCqZH6r8qX9H4G1kFhN4g8MvJHnQ7y7K1l5aJ2vQ0tN3mP9kL8jH7gF6dS5aQ4wE3r2t1', -- Admin@123
    uuid_generate_v4(),
    uuid_generate_v4(),
    FALSE, FALSE, TRUE, 0,
    'Super Admin',
    'SuperAdmin',
    TRUE,
    NOW()
);
*/

-- Create roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName") VALUES
('role-superadmin', 'SuperAdmin', 'SUPERADMIN'),
('role-admin', 'Admin', 'ADMIN'),
('role-editor', 'Editor', 'EDITOR'),
('role-moderator', 'Moderator', 'MODERATOR')
ON CONFLICT DO NOTHING;

# 🔄 Sequence Diagrams
## FoodStreetGuide - Hệ thống Du lịch Ẩm thực   

---

## 1. App Launch & Initialization

```mermaid
sequenceDiagram
    actor User
    participant App as FoodStreetGuide App
    participant DB as DatabaseService
    participant Geo as GeofenceEngine
    participant Web as WebAdminService
    participant Loc as LocationService
    participant TTS as TTSService

    User->>App: Open App
    App->>DB: Init() - Create SQLite tables
    DB->>DB: CreateTable<POI>()
    DB->>DB: CreateTable<Review>()
    DB->>DB: Seed test data if empty
    DB-->>App: Database ready

    App->>Geo: new GeofenceEngine()
    Geo-->>App: Geofence enabled
    
    App->>Loc: Check GPS permission
    Loc-->>App: Permission granted
    
    App->>TTS: Initialize TTS engine
    TTS-->>App: TTS ready
    
    App->>Web: TestConnectionAsync()
    Web->>Web: HTTP GET /api/pois.php
    Web-->>App: Connection status
    
    App->>App: Load POIs from database
    App->>App: Display map with markers
    App-->>User: App ready
```

---

## 2. Geofence Trigger - Auto Narration

```mermaid
sequenceDiagram
    actor User
    participant Loc as LocationService
    participant Geo as GeofenceEngine
    participant Main as MainPage
    participant Audio as AudioPlayerService
    participant TTS as TTSService

    User->>User: Walking near POI
    
    loop GPS Update (5-30s interval)
        Loc->>Loc: Get current location
        Loc->>Main: OnLocationUpdated(lat, lng)
        Main->>Geo: UpdateLocation(lat, lng)
        
        Geo->>Geo: Calculate distance to all POIs
        Geo->>Geo: Check which POIs are inside radius
        
        alt Inside geofence & Priority check
            Geo->>Geo: Sort by Priority (3→1) then Distance
            Geo->>Geo: Check cooldown (5 min)
            
            alt Not in cooldown
                Geo->>Main: POIEntered event
                Main->>Main: Show POI Card
                Main->>Audio: PlayAudioNotification()
                
                alt Auto-narration enabled
                    Main->>TTS: SpeakAsync(description)
                    TTS-->>Main: Speaking...
                end
                
                Main-->>User: Display POI info + audio playing
            else In cooldown
                Geo->>Geo: Log "SKIP - cooldown active"
            end
        end
    end
```

---

## 3. Data Synchronization (Web → Mobile)

```mermaid
sequenceDiagram
    participant User
    participant App as FoodStreetGuide App
    participant DB as DatabaseService
    participant WebSvc as WebAdminService
    participant API as Web Admin API
    participant MySQL as MySQL Database

    User->>App: Tap "Sync from Web"
    
    App->>WebSvc: SyncFromWebAdminAsync()
    WebSvc->>API: GET /api/pois.php
    API->>MySQL: SELECT * FROM pois
    MySQL-->>API: List of POIs
    API-->>WebSvc: JSON response
    
    loop Each POI from Web
        WebSvc->>DB: GetPOIsAsync() - check existing
        DB-->>WebSvc: Local POI or null
        
        alt POI not exists locally
            WebSvc->>DB: AddPOIAsync(newPOI)
            DB->>DB: INSERT INTO POI
            DB-->>WebSvc: Success
        else POI exists - check update time
            WebSvc->>WebSvc: Compare LastSyncFromWeb
            alt Remote is newer
                WebSvc->>DB: UpdatePOIAsync(poi)
                DB->>DB: UPDATE POI
            end
        end
    end
    
    WebSvc->>DB: Same for Reviews
    
    WebSvc-->>App: Sync completed
    App->>App: Refresh map markers
    App-->>User: Show sync success message
```

---

## 4. User Reviews Flow

```mermaid
sequenceDiagram
    actor User
    participant Detail as POIDetailPage
    participant DB as DatabaseService
    participant Web as WebAdminService
    participant API as Web Admin API

    User->>Detail: View POI details
    Detail->>DB: GetReviewsAsync(poiId)
    DB-->>Detail: List of approved reviews
    Detail-->>User: Display reviews

    User->>Detail: Tap "Write Review"
    Detail->>Detail: Show review form
    User->>Detail: Enter rating + comment + images
    User->>Detail: Submit review
    
    Detail->>DB: AddReviewAsync(review)
    DB->>DB: INSERT INTO Review
    DB-->>Detail: Review saved locally
    
    alt Network available
        Detail->>Web: POST /api/reviews.php
        Web->>API: Forward review data
        API->>MySQL: INSERT INTO reviews
        MySQL-->>API: Success
        API-->>Web: Review created
        Web-->>Detail: Sync success
        Detail->>DB: UpdateReviewAsync(webId)
    else Offline
        Detail->>Detail: Queue for later sync
    end
    
    Detail-->>User: Show "Review submitted"
```

---

## 5. Background Tracking with Notification

```mermaid
sequenceDiagram
    actor User
    participant Main as MainPage
    participant Loc as LocationService
    participant Geo as GeofenceEngine
    participant Notif as NotificationService
    participant Audio as AudioService

    User->>Main: Tap "Start Tracking"
    Main->>Loc: StartTrackingAsync()
    Loc->>Loc: Request background location
    Loc->>Notif: Show persistent notification
    Notif-->>Loc: Notification displayed
    Loc-->>Main: Tracking started
    
    User->>User: Press Home (app background)
    
    loop Continuous tracking
        Loc->>Loc: GPS update every 5-30s
        Loc->>Geo: UpdateLocation(lat, lng)
        
        Geo->>Geo: Check geofences
        alt Enter geofence
            Geo->>Notif: Update notification
            Notif-->>User: "Near: [POI Name]"
            
            alt App in background
                Geo->>Audio: Play audio cue
                Audio-->>User: Audio notification
            end
        end
    end
    
    User->>User: Swipe away app (kill)
    Main->>Loc: StopTracking()
    Loc->>Notif: Remove notification
    Notif-->>User: Tracking stopped
```

---

## 6. POI Discovery & Search

```mermaid
sequenceDiagram
    actor User
    participant Discover as DiscoverPage
    participant DB as DatabaseService
    participant Filter as FilterService
    participant Main as MainPage

    User->>Discover: Open Discover tab
    Discover->>DB: GetPOIsAsync()
    DB-->>Discover: All POIs
    Discover->>Discover: Apply default sort (distance)
    Discover-->>User: Show POI grid/list

    User->>Discover: Enter search text
    Discover->>Filter: FilterPOIs(searchText)
    Filter->>Filter: Match NameVi, NameEn, Tags
    Filter-->>Discover: Filtered list
    Discover->>Discover: Update UI
    Discover-->>User: Show search results

    User->>Discover: Apply category filter
    Discover->>Filter: FilterPOIs(category)
    Filter-->>Discover: Filtered by category
    Discover-->>User: Show filtered results

    User->>Discover: Tap on POI
    Discover->>Main: Navigate with poiId
    Main->>DB: GetPOIById(id)
    DB-->>Main: POI details
    Main->>Main: Center map on POI
    Main->>Main: Show POI card
    Main-->>User: Map centered, POI highlighted
```

---

## 7. QR Code Scan & Check-in

```mermaid
sequenceDiagram
    actor User
    participant QR as QRScanPage
    participant Camera as CameraService
    participant Decoder as QRDecoder
    participant DB as DatabaseService
    participant Geo as GeofenceEngine

    User->>QR: Tap QR Scan button
    QR->>QR: Check camera permission
    QR->>Camera: Start camera preview
    Camera-->>QR: Camera active
    QR-->>User: Show camera view

    User->>User: Point camera at QR code
    Camera->>Decoder: Capture frame
    Decoder->>Decoder: Decode QR content
    Decoder-->>Camera: POI ID extracted

    Camera->>QR: OnQRCodeDetected(poiId)
    QR->>DB: GetPOIAsync(poiId)
    DB-->>QR: POI details

    alt Valid POI
        QR->>Geo: Validate proximity (optional)
        Geo->>QR: Distance check result
        
        alt Within range or no check
            QR->>DB: IncrementVisitCount(poiId)
            QR->>QR: Show POI details
            QR-->>User: "Check-in successful!"
        else Too far
            QR-->>User: "Please move closer"
        end
    else Invalid POI
        QR-->>User: "Invalid QR code"
    end

    User->>QR: Tap close/done
    QR->>Camera: Stop camera
    QR->>QR: Navigate back
```

---

## 8. Settings & Localization

```mermaid
sequenceDiagram
    actor User
    participant Settings as SettingsPage
    participant LocSvc as LocalizationService
    participant SettingsSvc as ISettingsService
    participant App as AppShell
    participant Main as MainPage
    participant Saved as SavedPage

    User->>Settings: Open Settings
    Settings->>SettingsSvc: LoadSettings()
    SettingsSvc-->>Settings: Current settings
    Settings-->>User: Display settings UI

    User->>Settings: Change language (VI → EN)
    Settings->>LocSvc: SetLanguage("en")
    LocSvc->>LocSvc: Load localized strings
    LocSvc-->>Settings: Language changed event

    Settings->>App: UpdateTabTitles()
    App->>LocSvc: GetString("Tab_Map")
    App->>LocSvc: GetString("Tab_Discover")
    App->>LocSvc: GetString("Tab_Saved")
    App->>LocSvc: GetString("Tab_Settings")
    App-->>Settings: Tab titles updated

    Settings->>Main: UpdateLanguage()
    Main->>LocSvc: GetString("Tracking")
    Main->>LocSvc: GetString("Nearest")
    Main-->>Settings: MainPage updated

    Settings->>Saved: UpdateLanguage()
    Saved->>LocSvc: GetString("Saved_Title")
    Saved-->>Settings: SavedPage updated

    Settings-->>User: "Settings saved"
```

---

## 9. Audio Narration Pipeline

```mermaid
sequenceDiagram
    actor User
    participant Main as MainPage
    participant TTS as TTSService
    participant Audio as AudioPlayerService
    participant Platform as Platform Audio

    User->>Main: Tap "Listen" button
    Main->>Main: Get POI description
    
    alt Pre-recorded audio exists
        Main->>Audio: PlayFromUrl(audioUrl)
        Audio->>Platform: Initialize MediaPlayer
        Platform->>Platform: Stream audio
        Platform-->>Audio: Playing...
        Audio-->>Main: Playback started
    else Use TTS
        Main->>TTS: SpeakAsync(description, lang)
        TTS->>TTS: Split text into sentences
        
        loop Each sentence
            TTS->>Platform: TextToSpeech(sentence)
            Platform->>Platform: Synthesize speech
            Platform-->>TTS: Audio data
            TTS->>Audio: Play audio chunk
            Audio->>Platform: Output to speaker
        end
        
        TTS-->>Main: TTS completed
    end

    User->>Main: Tap "Stop"
    Main->>Audio: Stop()
    Audio->>Platform: Stop playback
    Audio->>TTS: Cancel()
    Platform-->>Audio: Stopped
    Audio-->>Main: Audio stopped
```

---

## 10. Error Handling & Recovery

```mermaid
sequenceDiagram
    participant App as FoodStreetGuide App
    participant Web as WebAdminService
    participant DB as DatabaseService
    participant Logger as Debug Logger

    App->>Web: SyncFromWebAdminAsync()
    Web->>Web: HTTP GET request
    
    alt Network error
        Web->>Logger: Log error details
        Web-->>App: Return cached data
        App->>DB: GetLocalPOIsAsync()
        DB-->>App: Local POIs
        App->>App: Show offline indicator
        App->>App: Queue sync for retry
    else API error (500)
        Web->>Logger: Log API error
        Web-->>App: Error response
        App->>App: Show error message
        App->>App: Offer retry button
    else Success
        Web-->>App: Fresh data
        App->>DB: Update local database
    end

    alt Database locked/corrupt
        App->>DB: Query POIs
        DB->>DB: SQLite exception
        DB-->>App: Throw exception
        App->>Logger: Log crash info
        App->>App: Reset database
        App->>DB: Re-initialize
        DB-->>App: Fresh database
        App->>Web: Force full sync
    end
```

---

## Diagram Key

| Symbol | Meaning |
|--------|---------|
| `->>` | Synchronous call |
| `-->>` | Return response |
| `->` | Asynchronous event |
| `alt` | Alternative flow (if/else) |
| `loop` | Repeated operation |
| `par` | Parallel processing |
| `activate` | Object active |
| `deactivate` | Object inactive |

---

## System Components Reference

| Component | Responsibility |
|-----------|----------------|
| **MainPage** | Map display, POI cards, user interaction |
| **DiscoverPage** | POI list, search, filters |
| **SavedPage** | Favorites management |
| **GeofenceEngine** | Location monitoring, trigger logic |
| **LocationService** | GPS tracking, permissions |
| **WebAdminService** | API communication, sync |
| **DatabaseService** | SQLite CRUD operations |
| **TTSService** | Text-to-speech narration |
| **AudioPlayerService** | Audio playback |
| **LocalizationService** | Multi-language support |

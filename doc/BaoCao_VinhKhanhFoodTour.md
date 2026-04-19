# BÁO CÁO ĐỒ ÁN: HỆ THỐNG HƯỚNG DẪN THUYẾT MINH VÀ QUẢN LÝ FOOD TOUR VĨNH KHÁNH

---

## 1. TỔNG QUAN HỆ THỐNG

### 1.1 Giới thiệu

**VinhKhanhFoodTour** là hệ thống hướng dẫn du lịch ẩm thực thông minh dành cho địa bàn Vĩnh Khánh, tỉnh An Giang. Hệ thống kết hợp công nghệ định vị GPS và phát thanh thuyết minh tự động để mang lại trải nghiệm tham quan các quán ăn đặc sản một cách sinh động, tiện lợi và không cần hướng dẫn viên trực tiếp.

### 1.2 Mục tiêu hệ thống

- Cung cấp nền tảng thuyết minh âm thanh tự động cho du khách khi đến gần các điểm ẩm thực (POI - Point of Interest).
- Giúp chủ quán ăn (Owner) tự quản lý nội dung quán và theo dõi mức độ tương tác.
- Cung cấp cho Admin trung tâm quản lý và phê duyệt nội dung toàn hệ thống.
- Hỗ trợ chế độ offline để đảm bảo trải nghiệm ngay cả khi mạng kém.

### 1.3 Các thành phần hệ thống

| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| **Backend API** | .NET 10, ASP.NET Core, Entity Framework, PostgreSQL | Cung cấp REST API trung tâm |
| **Admin Portal** | Blazor Server, TailwindCSS | Giao diện web quản trị |
| **Mobile App** | .NET MAUI (Android/iOS) | Ứng dụng dành cho du khách |
| **Database** | PostgreSQL + NetTopologySuite (GIS) | Lưu trữ dữ liệu có tọa độ địa lý |

---

## 2. PHÂN TÍCH TÁC NHÂN (ACTORS)

Hệ thống có **3 tác nhân chính**:

| Tác nhân | Mô tả | Nền tảng |
|---|---|---|
| **Du khách (Tourist)** | Người dùng ứng dụng di động, không cần đăng nhập hoặc đăng nhập dưới dạng khách | Mobile App (iOS/Android) |
| **Chủ quán (Owner)** | Chủ sở hữu các điểm ẩm thực, đăng ký tài khoản để quản lý quán | Admin Portal (Web) |
| **Quản trị viên (Admin)** | Người vận hành hệ thống, có toàn quyền kiểm soát | Admin Portal (Web) |

---

## 3. KIẾN TRÚC HỆ THỐNG

```mermaid
graph TB
    subgraph "Người dùng cuối"
        Tourist["🧑‍🦱 Du khách\n(Mobile App)"]
        Owner["🏪 Chủ quán\n(Web Portal)"]
        Admin["👨‍💼 Admin\n(Web Portal)"]
    end

    subgraph "Frontend"
        App["📱 MAUI Mobile App\n(Android / iOS)"]
        Portal["🌐 Blazor Admin Portal"]
    end

    subgraph "Backend"
        API["🖥️ ASP.NET Core REST API"]
        Auth["🔐 JWT Authentication"]
    end

    subgraph "Lưu trữ"
        DB[("🗄️ PostgreSQL\n+ PostGIS")]
        WWW["📂 wwwroot\n(Audio, Images,\nOffline Pack)"]
        SQLite["📦 SQLite Cache\n(Offline - Mobile)"]
    end

    Tourist --> App
    Owner --> Portal
    Admin --> Portal
    App -->|HTTP/REST| API
    Portal -->|HTTP/REST| API
    API --> Auth
    API --> DB
    API --> WWW
    App --> SQLite
```

---

## 4. MÔ HÌNH DỮ LIỆU

```mermaid
erDiagram
    User {
        int Id PK
        int RoleId FK
        string Username
        string PasswordHash
        string Email
        bool IsActive
        string PreferredLanguage
    }
    Role {
        int Id PK
        string RoleName
    }
    Poi {
        int Id PK
        int OwnerId FK
        string Name
        geometry Location
        double Latitude
        double Longitude
        string ImageUrl
        PoiStatus Status
        string RejectionReason
        int TriggerRadius
        datetime LastUpdated
    }
    PoiTranslation {
        int Id PK
        int PoiId FK
        string LanguageCode
        string Title
        string Description
        string AudioFilePath
        string ImageUrl
    }
    Tour {
        int Id PK
        string Title
        string Description
        decimal EstimatedPrice
        string ThumbnailUrl
        datetime CreatedAt
    }
    TourPoi {
        int TourId FK
        int PoiId FK
        int OrderIndex
    }
    NarrationLog {
        int Id PK
        int PoiId FK
        string DeviceId
        int ListenDurationSeconds
        datetime Timestamp
    }
    QrScanLog {
        int Id PK
        int PoiId FK
        string DeviceId
        datetime Timestamp
    }
    TourUsageLog {
        int Id PK
        int TourId FK
        string DeviceId
        datetime Timestamp
    }

    User ||--o{ Poi : "sở hữu"
    User }o--|| Role : "thuộc"
    Poi ||--o{ PoiTranslation : "có"
    Poi ||--o{ NarrationLog : "ghi nhận"
    Poi ||--o{ QrScanLog : "ghi nhận"
    Tour ||--o{ TourPoi : "gồm"
    Poi ||--o{ TourPoi : "thuộc"
    Tour ||--o{ TourUsageLog : "ghi nhận"
```

---

## 5. BIỂU ĐỒ USECASE

### 5.1 UseCase Tổng quan

```plantuml
@startuml UseCaseTongQuan
left to right direction
skinparam actorStyle awesome

actor "Du khách\n(Tourist)" as Tourist
actor "Chủ quán\n(Owner)" as Owner
actor "Quản trị viên\n(Admin)" as Admin

rectangle "Hệ thống VinhKhanhFoodTour" {
    ' --- TOURIST ---
    usecase "Xem trang chủ & Danh sách Tour" as UC_T1
    usecase "Nghe thuyết minh tự động (Geofence)" as UC_T2
    usecase "Quét mã QR để xem quán" as UC_T3
    usecase "Xem bản đồ các quán ăn" as UC_T4
    usecase "Tải dữ liệu offline" as UC_T5
    usecase "Chọn ngôn ngữ thuyết minh" as UC_T6
    usecase "Xem chi tiết quán ăn" as UC_T7
    usecase "Đi theo Tour có sẵn" as UC_T8

    ' --- OWNER ---
    usecase "Đăng nhập vào Portal" as UC_O0
    usecase "Thêm quán ăn mới" as UC_O1
    usecase "Sửa thông tin quán" as UC_O2
    usecase "Upload thuyết minh (Audio)" as UC_O3
    usecase "Xem thống kê lượt nghe" as UC_O4
    usecase "Xem thống kê quét QR" as UC_O5
    usecase "Tạo mã QR cho quán" as UC_O6
    usecase "Xem bản đồ quán của mình" as UC_O7

    ' --- ADMIN ---
    usecase "Phê duyệt / Từ chối quán ăn" as UC_A1
    usecase "Quản lý người dùng" as UC_A2
    usecase "Tạo và quản lý Tour" as UC_A3
    usecase "Xem bản đồ nhiệt hệ thống" as UC_A4
    usecase "Tạo gói dữ liệu Offline Pack" as UC_A5
    usecase "Xem dashboard tổng quan" as UC_A6
    usecase "Gợi ý tuyến đường Tour thông minh" as UC_A7

    Tourist --> UC_T1
    Tourist --> UC_T2
    Tourist --> UC_T3
    Tourist --> UC_T4
    Tourist --> UC_T5
    Tourist --> UC_T6
    Tourist --> UC_T7
    Tourist --> UC_T8

    Owner --> UC_O0
    Owner --> UC_O1
    Owner --> UC_O2
    Owner --> UC_O3
    Owner --> UC_O4
    Owner --> UC_O5
    Owner --> UC_O6
    Owner --> UC_O7

    Admin --> UC_A1
    Admin --> UC_A2
    Admin --> UC_A3
    Admin --> UC_A4
    Admin --> UC_A5
    Admin --> UC_A6
    Admin --> UC_A7
}
@enduml
```

---

### 5.2 Mô tả chi tiết các Use Case

#### UC-T2: Nghe thuyết minh tự động

| Trường | Nội dung |
|---|---|
| **Tên** | Nghe thuyết minh tự động (Geofence Auto-Play) |
| **Tác nhân** | Du khách (Tourist) |
| **Điều kiện trước** | App đã bật quyền truy cập GPS; Dữ liệu POI đã được tải |
| **Luồng chính** | 1. App theo dõi GPS liên tục; 2. Phát hiện người dùng vào vùng 30m của POI; 3. Hiển thị tên quán và phát thuyết minh (MP3 hoặc TTS); 4. Ghi log lượt nghe gửi về server |
| **Luồng thay thế** | Nếu đang nghe quán A: hiện popup hỏi có muốn nghe quán B ngay không; Nếu không: đưa vào hàng chờ |
| **Điều kiện sau** | Log lượt nghe được ghi vào database; Thống kê của Owner được cập nhật |

#### UC-O1: Thêm quán ăn mới

| Trường | Nội dung |
|---|---|
| **Tên** | Thêm quán ăn mới (POI) |
| **Tác nhân** | Chủ quán (Owner) |
| **Điều kiện trước** | Owner đã đăng nhập vào Admin Portal |
| **Luồng chính** | 1. Owner nhấn "Thêm quán ăn mới"; 2. Điền thông tin tên, mô tả, tọa độ, ảnh; 3. Hệ thống lưu quán với trạng thái "Chờ duyệt"; 4. Admin nhận yêu cầu phê duyệt |
| **Điều kiện sau** | Quán được tạo ở trạng thái Pending; Admin được thông báo để phê duyệt |

#### UC-A1: Phê duyệt quán ăn

| Trường | Nội dung |
|---|---|
| **Tên** | Phê duyệt / Từ chối quán ăn |
| **Tác nhân** | Quản trị viên (Admin) |
| **Điều kiện trước** | Có quán đang ở trạng thái "Chờ duyệt" |
| **Luồng chính** | 1. Admin vào Dashboard → xem danh sách Pending; 2. Xem thông tin quán; 3. Nhấn "Duyệt" hoặc "Từ chối" + lý do; 4. Hệ thống cập nhật trạng thái và tự động tạo lại gói Offline Pack |
| **Điều kiện sau** | Quán được công khai trên App (nếu duyệt) hoặc Owner được thông báo lý do từ chối |

---

## 6. BIỂU ĐỒ SEQUENCE (TUẦN TỰ)

### SQ-01: Luồng Khởi động App và Đồng bộ Offline

```mermaid
sequenceDiagram
    participant App as 📱 Mobile App
    participant Cache as 💾 SQLite Cache
    participant API as 🖥️ Backend API
    participant DB as 🗄️ Database

    App->>API: GET /Sync/public/version
    API->>DB: SELECT MAX(LastUpdated) FROM Pois
    DB-->>API: version = "20260419120000"
    API-->>App: { version: "20260419120000" }

    App->>Cache: GetCachedVersion()
    Cache-->>App: cachedVersion = "20260419000000"

    alt Phiên bản khác nhau
        App->>API: GET /Sync/public/sync/download-zip
        API-->>App: VinhKhanh_OfflinePack.zip
        App->>Cache: SavePoisAsync(pois)
        App->>App: Hiển thị "Đã cập nhật dữ liệu mới!"
    else Phiên bản khớp
        App->>App: Dùng dữ liệu Cache hiện tại
    end

    App->>Cache: GetCachedPoisAsync()
    Cache-->>App: List<Poi>
    App->>App: Render trang chủ
```

### SQ-02: Luồng Phát thuyết minh tự động (Geofence)

```mermaid
sequenceDiagram
    participant GPS as 📡 GPS Service
    participant GF as 🧭 GeofenceManager
    participant Audio as 🎵 AudioGuideService
    participant UI as 📱 UI (MainThread)
    participant API as 🖥️ Backend API

    GPS->>GF: LocationUpdated(lat, lng)
    GF->>GF: FindClosestEligiblePoi(location)

    alt Không có POI gần đó
        GF->>GF: return (không làm gì)
    else Tìm thấy POI trong vòng 30m
        alt Đang phát thuyết minh khác
            GF->>UI: ShowConfirmationDialog(newPoi)
            UI-->>GF: userChoice = true/false

            alt userChoice == true (Nghe ngay)
                GF->>Audio: StopAudioAsync()
                GF->>Audio: PlayAudioAsync(newPoi)
            else userChoice == false (Để sau)
                GF->>Audio: EnqueuePoi(newPoi)
            end
        else Không đang phát gì
            GF->>Audio: PlayAudioAsync(poi)
        end
    end

    Audio->>API: GET /Poi/public/{id} (nếu cần)
    API-->>Audio: PoiDetail với AudioFilePath

    alt Có file MP3
        Audio->>Audio: PlayMp3(audioUrl)
        Audio->>API: POST /Sync/logs (ghi thống kê)
        Audio-->>Audio: PlaybackEnded → ProcessQueueAsync()
    else Không có MP3 → TTS
        Audio->>Audio: TextToSpeech.SpeakAsync(text)
        Audio->>API: POST /Sync/logs (ghi thống kê)
        Audio-->>Audio: SpeakAsync done → ProcessQueueAsync()
    end
```

### SQ-03: Luồng Đăng nhập (Owner / Admin)

```mermaid
sequenceDiagram
    participant User as 👤 User (Browser)
    participant Portal as 🌐 Admin Portal
    participant API as 🖥️ Backend API
    participant DB as 🗄️ Database

    User->>Portal: Nhập Username + Password
    Portal->>API: POST /api/v1/Auth/login { username, password }
    API->>DB: SELECT User WHERE Username = ?
    DB-->>API: User record (PasswordHash, RoleId)
    API->>API: BCrypt.Verify(password, passwordHash)

    alt Đúng mật khẩu
        API->>API: Tạo JWT Token (7 ngày, chứa Role)
        API-->>Portal: { Token, Role, Username, Expiration }
        Portal->>Portal: Lưu Token vào localStorage
        Portal->>Portal: Redirect về trang tương ứng Role
    else Sai mật khẩu
        API-->>Portal: 401 Unauthorized
        Portal-->>User: Hiển thị "Sai tài khoản hoặc mật khẩu"
    end
```

### SQ-04: Luồng Phê duyệt quán ăn (Admin)

```mermaid
sequenceDiagram
    participant Admin as 👨‍💼 Admin (Browser)
    participant Portal as 🌐 Admin Portal
    participant API as 🖥️ Backend API
    participant DB as 🗄️ Database
    participant Sync as 🔄 SyncOrchestrator

    Admin->>Portal: Xem danh sách quán chờ duyệt
    Portal->>API: GET /api/v1/Poi/pending [JWT: Admin]
    API->>DB: SELECT * FROM Pois WHERE Status = 'Pending'
    DB-->>API: List<PendingPois>
    API-->>Portal: [{ id, name, location, ... }]
    Portal-->>Admin: Hiển thị danh sách

    Admin->>Portal: Nhấn "Duyệt" / "Từ chối"
    alt Phê duyệt
        Portal->>API: PUT /api/v1/Poi/{id}/approve [JWT: Admin]
        API->>DB: UPDATE Poi SET Status = 'Approved'
        API->>Sync: TryRefreshOfflinePackAsync()
        Sync->>DB: Lấy tất cả POI đã duyệt
        Sync->>Sync: Tạo lại file ZIP + SHA256 hash
        API-->>Portal: 200 OK
        Portal-->>Admin: "Đã duyệt thành công!"
    else Từ chối
        Portal->>API: PUT /api/v1/Poi/{id}/reject { reason } [JWT: Admin]
        API->>DB: UPDATE Poi SET Status = 'Rejected', RejectionReason = ?
        API->>Sync: TryRefreshOfflinePackAsync()
        API-->>Portal: 200 OK
        Portal-->>Admin: "Đã từ chối!"
    end
```

### SQ-05: Luồng Tạo Tour và Gợi ý Tuyến đường (Admin)

```mermaid
sequenceDiagram
    participant Admin as 👨‍💼 Admin
    participant Portal as 🌐 Admin Portal
    participant API as 🖥️ Backend API
    participant DB as 🗄️ Database

    Admin->>Portal: Mở Trang "Xây dựng Tour"
    Portal->>API: GET /api/v1/Poi/builder-pool?sortBy=popularity
    API->>DB: SELECT Pois + NarrationLogs + QrScanLogs counts
    DB-->>API: List<PoiSummaryDto> (sorted by popularity)
    API-->>Portal: Danh sách POI với điểm mức độ phổ biến

    Admin->>Portal: Chọn các POI và nhấn "Gợi ý tuyến đường"
    Portal->>API: POST /api/v1/Tour/suggest-route { poiIds: [...] }
    API->>DB: SELECT Pois WHERE Id IN (poiIds)
    API->>API: NearestNeighbourSort(pois) từ POI phổ biến nhất
    API-->>Portal: Danh sách POI theo thứ tự tối ưu

    Admin->>Portal: Xác nhận thứ tự và điền Title, mô tả
    Portal->>API: POST /api/v1/Tour { title, pois, estimatedPrice }
    API->>DB: INSERT Tour, INSERT TourPois (ordered)
    DB-->>API: Tour mới
    API-->>Portal: 201 Created { tourId }
    Portal-->>Admin: "Tour đã được tạo thành công!"
```

### SQ-06: Luồng Quét QR Code

```mermaid
sequenceDiagram
    participant Tourist as 🧑‍🦱 Du khách
    participant App as 📱 Mobile App
    participant API as 🖥️ Backend API
    participant DB as 🗄️ Database

    Tourist->>App: Mở Camera và quét mã QR
    App->>App: Parse Deep Link URL (vinhkhanh://poi/123)
    App->>App: Navigate to ProjectDetailPage(poiId=123)

    App->>API: GET /api/v1/Poi/public/123
    API->>DB: SELECT Poi + Translations WHERE Id = 123
    DB-->>API: Poi với thuyết minh
    API-->>App: PoiDetail { name, description, audio, ... }
    App-->>Tourist: Hiển thị thông tin quán

    App->>API: POST /api/v1/Sync/public/logs/scan { poiId: 123, deviceId }
    API->>DB: INSERT QrScanLog
    DB-->>API: OK
    Note over API,DB: Thống kê quét QR của Owner tăng lên
```

### SQ-07: Luồng Thêm Quán Ăn Mới (Owner)

```mermaid
sequenceDiagram
    participant Owner as 🏪 Chủ quán
    participant Portal as 🌐 Admin Portal
    participant API as 🖥️ Backend API
    participant DB as 🗄️ Database
    participant Storage as 📂 wwwroot

    Owner->>Portal: Nhấn "Thêm quán ăn mới"
    Owner->>Portal: Điền thông tin và chọn ảnh
    Portal->>API: POST /api/v1/Poi/owner/create (multipart/form-data)
    Note over Portal,API: Gửi: name, lat, lng, title, description, imageFile

    API->>Storage: Lưu imageFile → /media/pois/{id}.jpg
    API->>DB: INSERT Poi (Status = Pending, ImageUrl = relative_path)
    DB-->>API: Poi { id, name, status: Pending }
    API-->>Portal: { message: "Thêm thành công! Chờ Admin duyệt" }
    Portal-->>Owner: Thông báo thành công

    Note over Owner,DB: Quán sẽ hiển thị trên App khi Admin phê duyệt
```

---

## 7. BIỂU ĐỒ ACTIVITY (HOẠT ĐỘNG)

### AC-01: Du khách trải nghiệm Tour

```mermaid
flowchart TD
    Start([🚀 Mở App]) --> SplashCheck{Có dữ liệu offline?}
    SplashCheck -->|Không| DownloadPack[Tải Offline Pack từ Server]
    SplashCheck -->|Có| CheckVersion{Kiểm tra phiên bản}
    DownloadPack --> SaveCache[Lưu vào SQLite Cache]
    SaveCache --> ShowHome
    CheckVersion -->|Có bản mới| DownloadPack
    CheckVersion -->|Đã mới nhất| ShowHome[Hiển thị Trang chủ]

    ShowHome --> BrowseTours[Xem danh sách Tour]
    BrowseTours --> SelectTour[Chọn một Tour]
    SelectTour --> ViewTourDetail[Xem chi tiết Tour & Bản đồ]
    ViewTourDetail --> EnableAutoPlay{Bật "Tự động thuyết minh"?}
    EnableAutoPlay -->|Có| StartGPS[Bật theo dõi GPS]
    EnableAutoPlay -->|Không| ManualMode[Chế độ tự chọn]

    StartGPS --> WalkAround((🚶 Di chuyển))
    WalkAround --> NearPoi{Trong 30m của POI?}
    NearPoi -->|Không| WalkAround
    NearPoi -->|Có| IsPlaying{Đang phát thuyết minh?}

    IsPlaying -->|Không| PlayAudio[Phát thuyết minh]
    IsPlaying -->|Có| ShowConfirm[Hiển thị hộp thoại xác nhận]

    ShowConfirm --> UserChoose{Người dùng chọn?}
    UserChoose -->|Nghe ngay| StopAndPlay[Dừng cũ, Phát mới]
    UserChoose -->|Để sau| AddQueue[Thêm vào hàng chờ]
    UserChoose -->|Timeout| AddQueue

    AddQueue --> WalkAround
    StopAndPlay --> LogStats[Ghi log thống kê]
    PlayAudio --> LogStats
    LogStats --> WalkAround
```

### AC-02: Owner quản lý nội dung quán

```mermaid
flowchart TD
    Start([🔐 Đăng nhập Portal]) --> Authenticate{Xác thực JWT}
    Authenticate -->|Thất bại| ErrorMsg[Hiển thị lỗi] --> Start
    Authenticate -->|Thành công| Dashboard[Xem Dashboard]

    Dashboard --> Choose{Chọn hành động}

    Choose -->|Thêm quán| AddPoi[Mở form Thêm quán]
    AddPoi --> FillForm[Điền thông tin & upload ảnh]
    FillForm --> Validate{Dữ liệu hợp lệ?}
    Validate -->|Không| ShowErrors[Hiển thị lỗi validation] --> FillForm
    Validate -->|Có| SubmitPoi[Gửi lên API]
    SubmitPoi --> PendingStatus[Quán ở trạng thái Chờ duyệt]
    PendingStatus --> Dashboard

    Choose -->|Upload thuyết minh| SelectPoi[Chọn quán cần upload]
    SelectPoi --> SelectLang[Chọn ngôn ngữ thuyết minh]
    SelectLang --> UploadAudio[Upload file MP3]
    UploadAudio --> Dashboard

    Choose -->|Xem thống kê| ViewStats[Trang Thống kê tương tác]
    ViewStats --> SelectMode{Chọn chế độ}
    SelectMode -->|Lượt nghe| NarrationChart[Biểu đồ lượt nghe]
    SelectMode -->|Quét QR| QrChart[Biểu đồ quét QR]
    NarrationChart --> Dashboard
    QrChart --> Dashboard

    Choose -->|Tạo QR Code| ShowQrModal[Mở Modal tạo QR]
    ShowQrModal --> GenerateQr[Tạo mã QR]
    GenerateQr --> DownloadQr[Tải về / In ấn]
    DownloadQr --> Dashboard

    Choose -->|Xem bản đồ| ViewMap[Xem bản đồ vị trí quán]
    ViewMap --> Dashboard
```

### AC-03: Admin quản lý và phê duyệt hệ thống

```mermaid
flowchart TD
    Start([👨‍💼 Đăng nhập Admin]) --> ADash[Xem Admin Dashboard]
    ADash --> Stats[Tổng quan: Tổng POI, Lượt nghe, Lượt quét QR]

    ADash --> Action{Chọn hành động}

    Action -->|Phê duyệt| PendingList[Xem danh sách quán chờ duyệt]
    PendingList --> ReviewPoi[Xem thông tin từng quán]
    ReviewPoi --> Decide{Quyết định}
    Decide -->|Duyệt| ApprovePoi[Gọi API Approve]
    Decide -->|Từ chối| RejectPoi[Nhập lý do → Gọi API Reject]
    ApprovePoi --> RefreshPack[Tự động tạo lại Offline Pack]
    RejectPoi --> RefreshPack
    RefreshPack --> ADash

    Action -->|Quản lý người dùng| UserList[Xem danh sách người dùng]
    UserList --> UserActions{Hành động}
    UserActions -->|Kích hoạt/Vô hiệu hóa| ToggleUser[Cập nhật IsActive]
    UserActions -->|Đổi vai trò| ChangeRole[Cập nhật RoleId]
    ToggleUser --> ADash
    ChangeRole --> ADash

    Action -->|Xây dựng Tour| TourBuilder[Trang xây dựng Tour]
    TourBuilder --> PickPois[Chọn các POI từ pool]
    PickPois --> SuggestRoute[Gợi ý tuyến đường thông minh]
    SuggestRoute --> ConfirmOrder[Xác nhận hoặc điều chỉnh thứ tự]
    ConfirmOrder --> FillTourInfo[Nhập tên, mô tả, giá dự kiến]
    FillTourInfo --> SaveTour[Lưu Tour]
    SaveTour --> ADash

    Action -->|Xem bản đồ| MapPage[Trang Bản đồ hệ thống]
    MapPage --> MapMode{Chế độ bản đồ}
    MapMode -->|Pin Map| PinMap[Xem ghim vị trí theo trạng thái]
    MapMode -->|Heatmap| HeatMap[Xem mật độ người dùng]
    PinMap --> ADash
    HeatMap --> ADash

    Action -->|Tạo Offline Pack| GenPack[Gọi API Generate Pack]
    GenPack --> ZipCreated[Tạo file ZIP + SHA256 hash]
    ZipCreated --> ADash
```

### AC-04: Luồng Đăng ký Owner và Chờ hoạt động

```mermaid
flowchart TD
    Start([🌐 Truy cập Portal]) --> ClickRegister[Nhấn "Đăng ký"]
    ClickRegister --> FillInfo[Điền Username, Email, Password]
    FillInfo --> Validate{Hệ thống kiểm tra}
    Validate -->|Username đã tồn tại| DupUser[Báo lỗi Username] --> FillInfo
    Validate -->|Email đã tồn tại| DupEmail[Báo lỗi Email] --> FillInfo
    Validate -->|Hợp lệ| HashPwd[Hash mật khẩu BCrypt]
    HashPwd --> CreateUser[Tạo User với Role = Owner]
    CreateUser --> LoginNow[Đăng nhập ngay]
    LoginNow --> GetJWT[Nhận JWT Token]
    GetJWT --> OwnerDashboard[Vào Dashboard Chủ quán]
    OwnerDashboard --> StartAdding[Bắt đầu thêm quán ăn]
```

---

## 8. API ENDPOINTS TỔNG HỢP

### 8.1 Authentication

| Method | Endpoint | Role | Mô tả |
|---|---|---|---|
| POST | `/api/v1/Auth/login` | Public | Đăng nhập bằng username/password |
| POST | `/api/v1/Auth/register` | Public | Đăng ký tài khoản Owner |
| POST | `/api/v1/Auth/guest-login` | Public | Đăng nhập dưới dạng khách (Tourist) |

### 8.2 POI (Point of Interest)

| Method | Endpoint | Role | Mô tả |
|---|---|---|---|
| GET | `/api/v1/Poi` | Public | Lấy danh sách tất cả POI đã duyệt |
| POST | `/api/v1/Poi` | Owner, Admin | Tạo POI mới (JSON) |
| GET | `/api/v1/Poi/pending` | Admin | Danh sách POI chờ duyệt |
| PUT | `/api/v1/Poi/{id}/approve` | Admin | Duyệt POI |
| PUT | `/api/v1/Poi/{id}/reject` | Admin | Từ chối POI |
| GET | `/api/v1/Poi/public/{id}` | Public | Chi tiết POI công khai |
| POST | `/api/v1/Poi/owner/create` | Owner | Tạo POI với ảnh (multipart) |
| GET | `/api/v1/Poi/owner/my-pois` | Owner | Lấy quán của mình |
| GET | `/api/v1/Poi/builder-pool` | Admin, Owner | Pool POI cho Tour Builder |
| GET | `/api/v1/Poi/overview-pins` | Admin, Owner | Ghim bản đồ tổng quan |
| GET | `/api/v1/Poi/user-heatmap` | Admin | Dữ liệu bản đồ nhiệt |
| GET | `/api/v1/Poi/map` | Public | Ghim bản đồ công khai |

### 8.3 Tour

| Method | Endpoint | Role | Mô tả |
|---|---|---|---|
| GET | `/api/v1/Tour` | Admin | Lấy danh sách Tour (Admin) |
| POST | `/api/v1/Tour` | Admin | Tạo Tour mới |
| GET | `/api/v1/Tour/public` | Public | Danh sách Tour công khai |
| PUT | `/api/v1/Tour/{id}` | Admin | Cập nhật Tour |
| DELETE | `/api/v1/Tour/{id}` | Admin | Xóa Tour |
| POST | `/api/v1/Tour/suggest-route` | Admin, Owner | Gợi ý tuyến đường thông minh |
| POST | `/api/v1/Tour/{id}/log-usage` | Public | Ghi log sử dụng Tour |

### 8.4 Sync & Statistics

| Method | Endpoint | Role | Mô tả |
|---|---|---|---|
| GET | `/api/v1/Sync/public/version` | Public | Kiểm tra phiên bản dữ liệu |
| GET | `/api/v1/Sync/public/sync/download-zip` | Public | Tải Offline Pack |
| POST | `/api/v1/Sync/admin/sync/generate-pack` | Admin | Tạo Offline Pack |
| POST | `/api/v1/Sync/logs` | Public | Ghi nhận lượt nghe thuyết minh |
| POST | `/api/v1/Sync/public/logs/scan` | Public | Ghi nhận lượt quét QR |
| GET | `/api/v1/Sync/owner/stats/listens` | Owner | Thống kê lượt nghe/quét của Owner |
| GET | `/api/v1/Sync/owner/stats/listens/trend` | Owner | Biểu đồ xu hướng 30 ngày / 12 tháng |

---

## 9. KIẾN TRÚC XỬ LÝ THUYẾT MINH (AUDIO GUIDE)

Hệ thống thuyết minh hoạt động theo mô hình **Fallback 3 Lớp** để đảm bảo luôn có âm thanh dù điều kiện mạng như thế nào:

```mermaid
flowchart TD
    A[🎯 Kích hoạt Thuyết minh] --> L1{Lớp 1: File MP3}
    L1 -->|Có file audio| B[✅ Phát file MP3\ntừ server]
    L1 -->|Không có file| L2{Lớp 2: Text-to-Speech}
    
    L2 --> L2A{Có bản dịch\nngôn ngữ đang chọn?}
    L2A -->|Có| C[✅ TTS với\ntext bản dịch có sẵn]
    L2A -->|Không| L2B{Thử Auto-dịch\ntừ tiếng Việt}
    
    L2B -->|Thành công| D[✅ TTS với\ntext đã dịch]
    L2B -->|Thất bại| L2C[✅ TTS tiếng Việt\n(Fallback an toàn)]

    B --> Log[📊 Ghi log lượt nghe]
    C --> Log
    D --> Log
    L2C --> Log
    Log --> Queue{Còn trong hàng chờ?}
    Queue -->|Có| NextPoi[⏭️ Đợi 1s → Phát quán tiếp theo]
    Queue -->|Không| End([✅ Kết thúc])
```

---

## 10. KẾT LUẬN

### 10.1 Tính năng đã triển khai

| STT | Tính năng | Trạng thái |
|---|---|---|
| 1 | Xác thực JWT (Login/Register/Guest) | ✅ Hoàn thành |
| 2 | Phát thuyết minh tự động 3 lớp | ✅ Hoàn thành |
| 3 | Hàng chờ và xác nhận thuyết minh | ✅ Hoàn thành |
| 4 | Quản lý POI (CRUD) | ✅ Hoàn thành |
| 5 | Workflow phê duyệt quán | ✅ Hoàn thành |
| 6 | Quét mã QR Deep Link | ✅ Hoàn thành |
| 7 | Tạo và quản lý Tour | ✅ Hoàn thành |
| 8 | Gợi ý tuyến đường thông minh | ✅ Hoàn thành |
| 9 | Đồng bộ dữ liệu Offline | ✅ Hoàn thành |
| 10 | Thống kê lượt nghe / quét QR | ✅ Hoàn thành |
| 11 | Bản đồ ghim vị trí | ✅ Hoàn thành |
| 12 | Bản đồ nhiệt (Heatmap - Admin only) | ✅ Hoàn thành |
| 13 | Đa ngôn ngữ thuyết minh (vi, en, ja, ko) | ✅ Hoàn thành |
| 14 | Phân quyền theo Role (Tourist/Owner/Admin) | ✅ Hoàn thành |

### 10.2 Công nghệ sử dụng

| Lớp | Công nghệ |
|---|---|
| **Mobile App** | .NET MAUI 10, CommunityToolkit.Mvvm, SQLite, Plugin.Maui.Audio |
| **Admin Portal** | Blazor Server, TailwindCSS, Chart.js, Leaflet.js |
| **Backend API** | ASP.NET Core 10, Entity Framework Core, JWT Bearer |
| **Database** | PostgreSQL 15, NetTopologySuite (GIS) |
| **Bảo mật** | BCrypt password hashing, JWT RS256, Role-based authorization |
| **Dữ liệu địa lý** | PostGIS, NetTopologySuite, MAUI Maps |

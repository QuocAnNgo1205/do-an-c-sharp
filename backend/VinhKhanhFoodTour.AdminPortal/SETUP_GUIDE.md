# Vinh Khanh Food Tour - Web Portal Setup Guide

## ✅ Phase 1 & 2 Complete: Core Setup & Authentication

### 📋 What Has Been Implemented

#### **Folder Structure**
```
AdminPortal/
├── Services/
│   ├── Auth/
│   │   ├── AuthService.cs       ✅ Login/Logout logic, token management
│   │   ├── AuthState.cs         ✅ State management with cascading parameters
│   │   └── *.cs
│   ├── Http/
│   │   ├── ApiClient.cs         ✅ HTTP wrapper with JWT & 401 interceptor
│   │   └── ILocalStorageService.cs
│   ├── Owner/                    
│   └── Admin/
├── Models/
│   ├── Auth/                    ✅ LoginRequest, AuthToken, AuthUser
│   ├── Poi/                     ✅ PoiDto, CreatePoiRequest
│   └── Statistics/              ✅ ListenStats, SyncStatus
├── Components/
│   ├── Auth/
│   │   ├── LoginPage.razor      ✅ Login form with error handling
│   │   └── ProtectedRoute.razor ✅ Route protection by role
│   ├── Layout/
│   │   ├── AppLayout.razor      ✅ Main layout with sidebar & topnav
│   │   ├── Sidebar.razor        ✅ Role-based menu items
│   │   ├── TopNav.razor         ✅ User info & logout
│   │   └── EmptyLayout.razor    ✅ Clean layout for login
│   ├── Owner/
│   │   └── OwnerDashboard.razor ✅ Basic scaffold with UI
│   ├── Admin/
│   │   └── AdminDashboard.razor ✅ Basic scaffold with UI
│   ├── Pages/
│   │   ├── Home.razor           ✅ Smart router to dashboards
│   │   └── NotFound.razor
│   ├── App.razor                ✅ Root with auth initialization
│   └── _Imports.razor           ✅ All namespaces included
├── wwwroot/
│   └── app.css                  ✅ Tailwind CSS directives
├── tailwind.config.js           ✅ Tailwind configuration
├── Program.cs                   ✅ DI setup & service registration
└── VinhKhanhFoodTour.AdminPortal.csproj  ✅ Blazored.LocalStorage added
```

### 🔧 Core Features Implemented

#### **1. Authentication Service (AuthService.cs)**
- ✅ Login with username/password → JWT token
- ✅ Token extraction and storage in localStorage
- ✅ User claims parsing (Id, Username, Role)
- ✅ Token validation and expiration checking
- ✅ Session restoration on app load
- ✅ Logout with auth state cleanup

#### **2. HTTP Client Wrapper (ApiClient.cs)**
- ✅ Base URL configuration (`https://localhost:7123`)
- ✅ Automatic JWT token attachment to headers
- ✅ **401 Interceptor** → Auto logout & redirect to login
- ✅ Generic GET, POST, PUT methods with JSON serialization
- ✅ Multipart form-data support for file uploads
- ✅ Error handling and logging

#### **3. Auth State Management (AuthState.cs)**
- ✅ Centralized authentication state
- ✅ Cascading parameters for all components
- ✅ Role-based properties (`IsAdmin`, `IsOwner`)
- ✅ State change notifications
- ✅ Current user context

#### **4. UI Components**
- ✅ **LoginPage.razor** — Clean login form with error messages
- ✅ **Sidebar.razor** — Role-based menu (Owner/Admin specific items)
- ✅ **TopNav.razor** — User info badge & logout button
- ✅ **ProtectedRoute.razor** — Guard routes by authentication/role
- ✅ **OwnerDashboard.razor** — Scaffold for POI management & stats
- ✅ **AdminDashboard.razor** — Scaffold for approvals & sync status

#### **5. Styling**
- ✅ Tailwind CSS configured and integrated
- ✅ Responsive design ready
- ✅ Dark sidebar with modern UI
- ✅ Utility classes (buttons, cards, forms, tables, alerts)
- ✅ Modal & loading spinner styles

---

## 🚀 How It Works

### **Login Flow**
1. User visits `/login`
2. Enters credentials (username/password)
3. AuthService calls `POST /api/v1/Auth/login`
4. Backend returns JWT token
5. Token + user info stored in localStorage
6. AuthState updated → triggers UI re-render
7. User redirected to Home (`/`)

### **Protected Routes**
1. Home page checks `AuthState.IsAuthenticated`
2. If not authenticated → redirected to `/login` in App.razor `OnInitializedAsync`
3. If authenticated, role-based dashboard loads (Owner or Admin)
4. ProtectedRoute component enforces role requirements

### **API Requests**
1. Any API call → ApiClient attaches JWT from localStorage
2. If response is `401 Unauthorized`:
   - Token removed from localStorage
   - AuthState.Logout() called
   - User redirected to `/login` with refreshed page
3. All other errors logged and thrown

### **Session Persistence**
- On app startup, `App.razor` calls `AuthService.RestoreAuthAsync()`
- If valid token in localStorage → AuthState updated automatically
- No need to re-login if page is refreshed

---

## 📝 Test Credentials

From your API seeding:
```
Admin:  username: admin,  password: 0
Owner:  username: owner1, password: 1
```

---

## 🔄 Phase 3 & 4: Ready for Implementation

### **Next Steps — Owner Dashboard (Phase 3)**

**Components to Create:**
- `RestaurantList.razor` — Table of POIs with pagination
- `AddRestaurantModal.razor` — Form with multi-part file upload
  - Fields: Name, Lat, Lon, TriggerRadius
  - File uploads: Image, Vietnamese MP3, English MP3
  - Form validation with error messages
- `StatisticsChart.razor` — Chart visualization of listen stats

**Services to Create:**
- `IOwnerService.cs` / `OwnerService.cs`
  - `GetMyPoisAsync()` → Call `GET /api/v1/Poi/owner/pois`
  - `GetListenStatsAsync()` → Call `GET /api/v1/Sync/owner/stats/listens`
  - `CreatePoiAsync(request)` → Call `POST /api/v1/Poi` with form data
  - `UpdatePoiAsync(id, request)` → Call `PUT /api/v1/Poi/{id}`

**Integration Points:**
- Chart library (recommend: Chart.js via Blazor wrapper or Plotly.js)
- File upload progress tracking
- Form validation library (built-in Blazor forms)

---

### **Next Steps — Admin Dashboard (Phase 4)**

**Components to Create:**
- `PendingApprovals.razor` — List pending POIs (Status = "Pending")
- `RejectModal.razor` — Modal with rejection reason input
- `SyncStatus.razor` — Display sync status card

**Services to Create:**
- `IAdminService.cs` / `AdminService.cs`
  - `GetPendingPoisAsync()` → Call `GET /api/v1/Poi/admin/pending`
  - `ApprovePoisAsync(id)` → Call `PUT /api/v1/Poi/admin/approve`
  - `RejectPoiAsync(id, reason)` → Call `PUT /api/v1/Poi/admin/reject`
  - `GetSyncStatusAsync()` → Call `GET /api/v1/Sync/admin/sync/status`
  - `GenerateSyncPackAsync()` → Call `POST /api/v1/admin/sync/generate-pack`

**Integration Points:**
- Confirmation dialogs before approve/reject
- Toast notifications for success/error feedback
- Real-time or periodic sync status polling

---

## 🛠️ Development Setup

### **Prerequisites**
- .NET 10 SDK installed
- Visual Studio Code or Visual Studio 2024
- Blazor WebAssembly/Server workload

### **First Run**
1. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the Admin Portal:
   ```bash
   cd VinhKhanhFoodTour.AdminPortal
   dotnet run
   ```

4. Navigate to:
   ```
   https://localhost:5001
   ```
   (Port may vary; check console output)

5. Login with test credentials above

### **Project Architecture**
- **Blazor Server** with Interactive components
- **Cascading Parameters** for auth state management
- **Dependency Injection** for services
- **localStorage** via Blazored.LocalStorage for persistence
- **Tailwind CSS** for styling

---

## 📚 File Reference

### **Critical Services** (Must implement in Phase 3 & 4)
- [ ] `Services/Owner/IOwnerService.cs` & `OwnerService.cs`
- [ ] `Services/Admin/IAdminService.cs` & `AdminService.cs`

### **Components to Enhance** (Phase 3)
- [ ] Update `Components/Owner/OwnerDashboard.razor` — Hook up API calls
- [ ] Create `Components/Owner/RestaurantList.razor`
- [ ] Create `Components/Owner/AddRestaurantModal.razor`
- [ ] Create `Components/Owner/StatisticsChart.razor`

### **Components to Enhance** (Phase 4)
- [ ] Update `Components/Admin/AdminDashboard.razor` — Hook up API calls
- [ ] Create `Components/Admin/PendingApprovals.razor`
- [ ] Create `Components/Admin/RejectModal.razor`
- [ ] Create `Components/Admin/SyncStatus.razor`

---

## 🐛 Debugging Tips

### **Check Auth State**
Add to any component:
```razor
<div class="debug-info">
    Authenticated: @AuthState.IsAuthenticated
    User: @AuthState.CurrentUser?.Username
    Role: @AuthState.CurrentUser?.Role
</div>
```

### **Check Storage**
Open browser DevTools → Application → Local Storage → `https://localhost:...`
Look for keys:
- `authToken` — JWT token
- `authUser` — Serialized AuthUser object

### **Check Network Requests**
Open browser DevTools → Network tab → Apply filter: `fetch`
All API calls should include:
```
Authorization: Bearer <token>
```

---

## 📦 NuGet Packages Added
- `Blazored.LocalStorage` — v4.5.0 — Client-side storage

---

## 🎯 Architecture Decisions

### **Why Cascading Parameters for Auth?**
- Blazor native approach (no external state management library)
- Automatic re-rendering on state change
- Type-safe DI injection

### **Why ApiClient over Microsoft.AspNetCore.Http.HttpClient?**
- Centralized JWT token attachment
- Uniform error handling & logging
- 401 interceptor for auto-logout
- Serialization/deserialization abstraction

### **Why Blazored.LocalStorage?**
- Industry standard for Blazor apps
- Handles JSON serialization transparently
- Clean async API
- Avoids JS interop complexity

### **Why Tailwind CSS?**
- Utility-first approach → faster development
- No class naming conflicts
- Highly customizable
- Built-in responsive design
- Small production bundle (with PurgeCSS)

---

## ✨ Code Quality Notes

- **Clean Code**: Services are focused, single responsibility
- **DRY**: Reusable components (Sidebar, TopNav, ProtectedRoute)
- **Error Handling**: Try-catch blocks in services + user-friendly messages
- **Logging**: ILogger injected in all services
- **Null Safety**: `#nullable enable` in all C# files
- **Async/Await**: All API calls are async
- **Validation**: LoginPage includes basic validation

---

## 🚦 Status

- ✅ Phase 1 — Core Setup (100%)
- ✅ Phase 2 — Auth & Layout (100%)
- ⏳ Phase 3 — Owner Dashboard (Ready for implementation)
- ⏳ Phase 4 — Admin Dashboard (Ready for implementation)

---

**Last Updated**: April 4, 2026
**Version**: 1.0.0-alpha

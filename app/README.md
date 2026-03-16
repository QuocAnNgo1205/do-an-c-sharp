# Vinh Khanh Food Tour - Mobile App Proof of Concept

A .NET MAUI mobile application demonstrating background location tracking with automated Text-to-Speech (TTS) narration when the user approaches Points of Interest (POIs) in Ho Chi Minh City.

## Project Architecture

The application follows the **MVVM (Model-View-ViewModel)** pattern with strict **Object-Oriented Programming** principles and **Dependency Injection**.

```
VinhKhanhFoodTour/
├── Models/
│   ├── PoiModel.cs                 # POI data model
│   └── NarrationLogEntry.cs        # Narration history log entry
├── Interfaces/
│   ├── ILocationService.cs         # GPS tracking abstraction
│   └── INarrationService.cs        # Geofence & TTS abstraction
├── Services/
│   ├── LocationService.cs          # GPS tracking implementation
│   └── NarrationService.cs         # Geofence & TTS implementation
├── ViewModels/
│   └── LocationTrackingViewModel.cs # Main business logic
├── Views/
│   ├── MainPage.xaml               # UI layout
│   └── MainPage.xaml.cs            # Code-behind
├── App.xaml                         # App resources
├── App.xaml.cs                      # App startup
├── MauiProgram.cs                  # Dependency injection setup
└── VinhKhanhFoodTour.csproj         # Project configuration
```

## Key Components

### 1. Models (`Models/`)

#### PoiModel.cs
Represents a Point of Interest with:
- **Id**: Unique identifier
- **Name**: POI name (e.g., "Ben Thanh Market")
- **Latitude/Longitude**: Geographic coordinates
- **DescriptionText**: Text to be narrated via TTS
- **Radius**: Activation distance in meters (geofence radius)

#### NarrationLogEntry.cs
Logs narration events displayed in the UI:
- POI name and timestamp
- Narration text
- User's location when narration triggered

### 2. Interfaces (`Interfaces/`)

#### ILocationService
Abstracts device GPS functionality:
```csharp
Task<Location?> GetCurrentLocationAsync();
Task<bool> CheckAndRequestPermissionAsync();
Task<bool> IsLocationServiceEnabledAsync();
```

#### INarrationService
Abstracts geofencing and TTS:
```csharp
double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
bool ShouldTriggerNarration(PoiModel poi, double lat, double lon);
Task PlayNarrationAsync(string text, CancellationToken ct);
```

### 3. Services (`Services/`)

#### LocationService.cs
- Uses `Microsoft.Maui.Devices.Sensors.Geolocation`
- Handles location permissions
- Validates location service availability
- Returns current GPS coordinates

#### NarrationService.cs
**Haversine Formula Implementation** (Distance Calculation):
```
Used to calculate great-circle distance between two geographic points:
a = sin²(Δφ/2) + cos φ1 ⋅ cos φ2 ⋅ sin²(Δλ/2)
c = 2 ⋅ atan2(√a, √(1−a))
d = R ⋅ c

Where:
- φ = latitude, λ = longitude, R = Earth's radius (6371 km)
- Returns distance in meters
```

**Cooldown Logic** (Prevents Audio Spam):
```csharp
// Check if POI was triggered before
if (_poiLastTriggeredTimes.TryGetValue(poi.Id, out var lastTriggerTime))
{
    TimeSpan timeSinceLastTrigger = DateTime.UtcNow - lastTriggerTime;
    
    // Skip if within 5-minute cooldown
    if (timeSinceLastTrigger.TotalMinutes < 5)
        return false;
}
```

Features:
- Distance calculation using Haversine formula
- Geofence checking (is user within POI radius?)
- 5-minute cooldown mechanism per POI
- TTS playback via `Microsoft.Maui.Media.TextToSpeech`

### 4. ViewModel (`ViewModels/`)

#### LocationTrackingViewModel.cs
Main business logic:
- **Timer-Based Tracking**: Requests location every 5 seconds
- **Geofence Detection**: Checks all POIs against user location
- **Narration Trigger**: Initiates TTS when conditions are met
- **Log Management**: Maintains narration history for UI display

**Workflow**:
```
1. User taps "Start Tracking"
   ↓
2. Permission check & service validation
   ↓
3. Start 5-second location polling timer
   ↓
4. Every 5 seconds:
   - Get current GPS location
   - Update UI with coordinates
   - Check each POI:
     * Calculate distance to POI
     * Check if within geofence radius
     * Check if not in cooldown period
   - For each eligible POI:
     * Log the event
     * Mark POI as triggered (start cooldown)
     * Play TTS narration
   ↓
5. User taps "Stop Tracking"
   - Dispose timer
   - Cancel ongoing TTS
   - Stop location updates
```

### 5. UI (`Views/`)

#### MainPage.xaml
Clean, simple interface:
- **Start/Stop Tracking Buttons**: Control location tracking
- **Current Location Display**: Shows GPS coordinates (Lat/Lon)
- **Status Message**: Updates user on app state
- **Narration Log**: ListView of triggered narrations with:
  - POI name and timestamp
  - Narration text preview
  - User's location at trigger time

## Mock Data

Three POIs initialized in Ho Chi Minh City (District 4):

1. **Ben Thanh Market**
   - Latitude: 10.7725, Longitude: 106.6992
   - Radius: 100m
   - Description: Historic market since 1914

2. **Bitexco Financial Tower**
   - Latitude: 10.7598, Longitude: 106.7031
   - Radius: 120m
   - Description: Vietnam's tallest building

3. **Dong Khoi Street**
   - Latitude: 10.7700, Longitude: 106.7050
   - Radius: 150m
   - Description: Premier shopping district

## Dependency Injection Setup

`MauiProgram.cs` registers services:
```csharp
.Services
    .AddSingleton<ILocationService, LocationService>()
    .AddSingleton<INarrationService, NarrationService>()
    .AddSingleton<LocationTrackingViewModel>()
    .AddSingleton<MainPage>()
```

**Why singletons?**
- Services maintain state (cooldown timers)
- ViewModel maintains UI state and timer
- Only one instance needed for entire app lifecycle

## Configuration

`appsettings.json`:
```json
{
  "AppSettings": {
    "LocationUpdateIntervalMs": 5000,    // Check POIs every 5s
    "PoiCooldownMinutes": 5,             // Cooldown duration
    "DefaultPoiActivationRadiusMeters": 100, // Default geofence
    "TtsLanguage": "en",
    "TtsSpeed": 1.0
  }
}
```

## Permissions & Requirements

### Android
- `android.permission.ACCESS_FINE_LOCATION` - Precise GPS location
- `android.permission.ACCESS_COARSE_LOCATION` - Approximate location (fallback)
- `android.permission.INTERNET` - For background operations

### Windows
- Location services must be enabled in system settings

### Runtime Permissions
The app requests `Permissions.LocationWhenInUse` at runtime, displaying permission dialog to user.

## Running the Application

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 with MAUI workload
- Android SDK (API 21+) for Android testing
- Windows 10/11 for Windows testing

### Setup
```bash
# Navigate to project directory
cd VinhKhanhFoodTour

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run on Windows
dotnet run -f net8.0-windows

# Run on Android
dotnet run -f net8.0-android
```

### Testing on Desktop
1. Launch the application
2. Click "Start Tracking"
3. The app simulates location updates every 5 seconds
4. For real testing, use device location or GPS simulator

## Code Quality & Design Patterns

### Object-Oriented Principles
- **Single Responsibility**: Each class has one reason to change
  - `LocationService`: Only handles GPS
  - `NarrationService`: Only handles distance calc and TTS
  - `ViewModel`: Only coordinates business logic

- **Open/Closed**: Code open for extension, closed for modification
  - Use interfaces for services (easy to swap implementations)
  - Mock services for testing

- **Dependency Inversion**: Depend on abstractions, not implementations
  - ViewModel depends on `ILocationService`, not `LocationService`
  - Easy to inject test doubles

### Clean Code Practices
- Comprehensive XML documentation comments
- Meaningful variable names
- Small, focused methods
- Error handling with try-catch blocks
- Separation of concerns (Models, Views, ViewModels, Services)

## Testing Considerations

### Unit Testing
Mock the interfaces for easy testing:
```csharp
// Mock location service
var mockLocationService = new Mock<ILocationService>();
mockLocationService
    .Setup(x => x.GetCurrentLocationAsync())
    .ReturnsAsync(new Location { Latitude = 10.7725, Longitude = 106.6992 });

// Mock narration service
var mockNarrationService = new Mock<INarrationService>();

// Create ViewModel with mocks
var viewModel = new LocationTrackingViewModel(mockLocationService.Object, mockNarrationService.Object);
```

### Integration Testing
- Use real GPS (requires actual device or emulator)
- Test location permission flows
- Verify TTS playback on device
- Validate cooldown mechanism

## Performance Considerations

### Location Updates
- **Every 5 seconds**: Balances responsiveness with battery usage
- For production, consider battery level and user settings

### Geofence Checks
- $O(n)$ complexity where n = number of POIs
- Efficient for small POI lists (<100)
- For large datasets, consider spatial indexing (quadtree, R-tree)

### Memory Management
- Timer is disposed properly
- No event handler leaks
- Collections use ObservableCollection for memory efficiency

## Future Enhancements

1. **Background Execution**: Implement background location tracking
2. **Database**: Replace mock data with SQLite/Cloud storage
3. **Real-time POI Updates**: Fetch POIs from server
4. **User Preferences**: Remember last location, tracking settings
5. **Analytics**: Track user interactions and POI visits
6. **Advanced Geofencing**: Multiple geofence states (entering, inside, exiting)
7. **Offline Support**: Cache POI data locally
8. **Multi-language Support**: Localize TTS based on user language
9. **Map Integration**: Show POIs on interactive map
10. **Custom Narration**: Allow users to record custom descriptions

## Architecture Diagram

```
┌─────────────────────────────────┐
│        MainPage (XAML UI)       │
│  ┌──────────────────────────┐   │
│  │ Start/Stop Buttons       │   │
│  │ Location Display         │   │
│  │ Narration Log List       │   │
│  └──────────────────────────┘   │
└────────────┬────────────────────┘
             │ (Data Binding)
             ↓
┌─────────────────────────────────┐
│ LocationTrackingViewModel       │
│ ┌─────────────────────────────┐ │
│ │ Timer (5-second polling)    │ │
│ │ POI Geofence Detection      │ │
│ │ Narration Trigger Logic     │ │
│ │ Log Management              │ │
│ └─────────────────────────────┘ │
└────────┬──────────────┬──────────┘
         │              │
    ┌────▼──────┐  ┌────▼────────────┐
    │Location    │  │INarration       │
    │Service     │  │Service          │
    ├────────────┤  ├─────────────────┤
    │ GPS        │  │ Distance Calc   │
    │ Permission │  │ Geofence Check  │
    │ Validation │  │ Cooldown Logic  │
    │            │  │ TTS Playback    │
    └────────────┘  └─────────────────┘
```

## License
This is a Proof of Concept for university-level project demonstration.

## Contact
For questions or improvements, please contact the development team.

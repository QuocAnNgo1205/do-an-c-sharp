# Quick Start Guide - Vinh Khanh Food Tour PoC

## 5-Minute Setup

### Prerequisites
- **Operating System**: Windows, macOS, or Linux
- **.NET SDK**: Version 8.0 or later
  - Download: https://dotnet.microsoft.com/download/dotnet
- **Code Editor**: Visual Studio 2022 or VS Code

### Installation Steps

#### 1. Install .NET MAUI Workload
```powershell
dotnet workload restore
dotnet workload install maui
```

#### 2. Clone/Setup Project
```powershell
cd e:\code\c#\demo2
```

#### 3. Restore Dependencies
```powershell
dotnet restore
```

#### 4. Build Project
```powershell
dotnet build
```

#### 5. Run Application

**On Windows:**
```powershell
dotnet run -f net8.0-windows
```

**On Android (requires Android SDK):**
```powershell
dotnet run -f net8.0-android
```

## Initial Usage

1. **Launch App**
   - App opens with "Ready to track" status

2. **Grant Location Permission**
   - Permission dialog appears on first run
   - Click "Allow" or "Grant"

3. **Start Tracking**
   - Click "Start Tracking" button
   - Status changes to "Tracking active..."

4. **Simulate Location** (Desktop Testing)
   - On Windows: Use location emulator in VS debugger
   - On Android: Use Android emulator's location simulation
   - Navigate to mock POI coordinates (see README.md)

5. **Listen for Narration**
   - When you approach a POI (within radius), TTS automatically plays
   - Narration appears in log
   - Same POI won't trigger for 5 minutes (cooldown)

6. **Stop Tracking**
   - Click "Stop Tracking" button
   - Location updates stop, TTS cancels

## Project Structure at a Glance

```
Models/
├── PoiModel.cs                    # POI definition
└── NarrationLogEntry.cs           # Log entry for narrations

Interfaces/
├── ILocationService.cs            # GPS abstraction
└── INarrationService.cs           # Geofence & TTS abstraction

Services/
├── LocationService.cs             # GPS implementation
└── NarrationService.cs            # Distance calc & TTS

ViewModels/
└── LocationTrackingViewModel.cs   # Business logic

Views/
├── MainPage.xaml                  # UI layout
└── MainPage.xaml.cs              # UI code-behind

App Files/
├── App.xaml                       # App resources
├── App.xaml.cs                   # App startup
└── MauiProgram.cs                # DI configuration

Configuration/
├── VinhKhanhFoodTour.csproj       # Project file
├── appsettings.json               # Settings
├── README.md                      # Full documentation
└── TECHNICAL_DEEP_DIVE.md         # Deep technical details
```

## Common Tasks

### Change POI Location
**File**: `ViewModels/LocationTrackingViewModel.cs`

Find the `InitializeMockPois()` method and modify coordinates:
```csharp
new PoiModel
{
    Id = 1,
    Name = "My Favorite Place",
    Latitude = 10.7000,      // ← Change this
    Longitude = 106.7000,    // ← Change this
    DescriptionText = "Welcome to my place!",
    Radius = 100
}
```

### Adjust Polling Interval
**File**: `ViewModels/LocationTrackingViewModel.cs`

```csharp
private const int LOCATION_UPDATE_INTERVAL_MS = 5000;  // ← Change (in milliseconds)
```
- 1000 = Every 1 second (battery intensive)
- 5000 = Every 5 seconds (default)
- 10000 = Every 10 seconds (battery friendly)

### Adjust Cooldown Duration
**File**: `Services/NarrationService.cs`

```csharp
private const int COOLDOWN_DURATION_MINUTES = 5;  // ← Change this
```

### Change Logger Verbosity
**File**: `appsettings.json`

```json
"Logging": {
  "LogLevel": {
    "Default": "Debug"    // ← Change to Debug, Information, Warning, Error
  }
}
```

## Debugging

### In Visual Studio
1. Open `VinhKhanhFoodTour.sln`
2. Set breakpoints in ViewModel or Services
3. Press F5 or click Run
4. Debug tools appear automatically

### Debug Output Window
Track background operations:
```csharp
// Use this in code
System.Diagnostics.Debug.WriteLine($"Message: {value}");

// See output in VS Debug console (Ctrl+Shift+Y)
```

### Common Issues

**Issue**: Permission dialog not appearing
- **Solution**: Clear app data and reinstall
  ```powershell
  dotnet clean
  dotnet build
  dotnet run -f net8.0-windows
  ```

**Issue**: No location updates
- **Solution**: 
  - Verify location services enabled on device
  - Check location permission in app settings
  - On Android emulator: Open emulator settings and set location

**Issue**: TTS not playing
- **Solution**:
  - Verify device has speakers/audio output
  - Check system text-to-speech is installed
  - Test with device text-to-speech settings

**Issue**: Endless narration (no cooldown)
- **Solution**: Check if `MarkPoiAsTriggered()` is called after narration
  - File: `ViewModels/LocationTrackingViewModel.cs`
  - Method: `TriggerNarrationAsync()`

## Testing with Mock Data

### Three Built-in POIs (Ho Chi Minh City)

1. **Ben Thanh Market** (District 1)
   - Lat: 10.7725°N, Lon: 106.6992°E
   - Radius: 100m

2. **Bitexco Financial Tower** (District 1) 
   - Lat: 10.7598°N, Lon: 106.7031°E
   - Radius: 120m

3. **Dong Khoi Street** (District 1)
   - Lat: 10.7700°N, Lon: 106.7050°E
   - Radius: 150m

### Desktop Testing
Use your IDE's location simulator:

**Visual Studio:**
- Debug → Windows → Location Simulator
- Set coordinates to match POI
- Move slowly within radius to trigger

**Android Emulator:**
- Extended Controls → Location
- Input coordinates
- Play simulated route

## Performance Tips

- **For slow devices**: Increase polling interval to 10 seconds
- **For battery life**: Increase polling to 15+ seconds between checks
- **For better accuracy**: Decrease radius below 100m (requires accurate GPS)
- **For fewer false positives**: Increase radius to 150m+

## Next Steps

1. **Read Full Documentation**
   - Open `README.md` for complete architecture overview

2. **Understand Deep Concepts**
   - Open `TECHNICAL_DEEP_DIVE.md` for Haversine & cooldown details

3. **Explore Code**
   - Start with `ViewModels/LocationTrackingViewModel.cs` (main logic)
   - Then review Services (`LocationService`, `NarrationService`)
   - Finally check UI in `Views/MainPage.xaml`

4. **Try Modifications**
   - Add more POIs
   - Change narration text
   - Adjust geofence radius
   - Modify UI layout

5. **Deploy for Real Testing**
   - Build APK for Android testing on real device
   - Use actual GPS instead of simulator

## Support Commands

```powershell
# Check .NET version
dotnet --version

# List installed MAUI workloads
dotnet workload list

# Clean project (if build fails)
dotnet clean

# Run specific debug configuration
dotnet run --configuration Debug

# Build release version
dotnet build --configuration Release

# Check project structure
dotnet workload list
```

## Project Statistics

- **Languages**: C#, XAML, JSON
- **Lines of Code**: ~1,500 (excluding docs)
- **Classes**: 7 (Models, Services, ViewModel)
- **Interfaces**: 2 (ILocationService, INarrationService)
- **External Dependencies**: Microsoft.Maui
- **Build Time**: ~30-60 seconds
- **App Size**: ~50-150 MB (platform-dependent)

## Resources

- **Official MAUI Documentation**: https://learn.microsoft.com/dotnet/maui/
- **Geolocation API**: https://learn.microsoft.com/dotnet/maui/fundamentals/geolocation
- **Text-to-Speech**: https://learn.microsoft.com/dotnet/maui/fundamentals/texttospeech
- **MVVM Guide**: https://learn.microsoft.com/dotnet/maui/fundamentals/mvvm
- **Haversine Formula**: https://en.wikipedia.org/wiki/Haversine_formula

---

**Ready to run? Execute these commands:**

```powershell
cd e:\code\c#\demo2
dotnet restore
dotnet build
dotnet run -f net8.0-windows
```

**Expected Output:**
- Windows desktop app opens
- "Grant permission" dialog appears
- Click "Allow"
- Click "Start Tracking"
- Watch console for "Tracking active..." message
- Simulate location to see narration!

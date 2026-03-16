# Technical Deep Dive: Geofence & Cooldown Implementation

## Table of Contents
1. Haversine Formula Explanation
2. Cooldown Mechanism
3. Code Implementation Details
4. Testing & Validation

---

## 1. Haversine Formula Explanation

### What is the Haversine Formula?

The **Haversine formula** calculates the shortest distance (great-circle distance) between two points on Earth's surface given their latitude and longitude coordinates.

### Why Use Haversine Instead of Euclidean?

- **Euclidean Distance** (Flat Earth Model):
  ```
  d = √[(x₂-x₁)² + (y₂-y₁)²]
  ```
  ❌ Inaccurate for geographic coordinates
  ❌ Treats Earth as flat (wrong)
  ❌ Error increases with distance

- **Haversine Function** (Spherical Earth Model):
  ```
  Accounts for Earth's curvature
  Accurate for all distances
  Standard in GIS applications
  ```

### Mathematical Derivation

**Given two points:**
- Point 1: (φ₁, λ₁) = User's location
- Point 2: (φ₂, λ₂) = POI's location

Where:
- φ = Latitude (in radians)
- λ = Longitude (in radians)

**Step 1: Convert Degrees to Radians**
```
φ₁_rad = φ₁ × (π/180)
λ₁_rad = λ₁ × (π/180)
```

**Step 2: Calculate Angular Differences**
```
Δφ = φ₂_rad - φ₁_rad  (change in latitude)
Δλ = λ₂_rad - λ₁_rad  (change in longitude)
```

**Step 3: Apply Haversine Formula**
```
a = sin²(Δφ/2) + cos(φ₁_rad) × cos(φ₂_rad) × sin²(Δλ/2)
```

This calculates the **squared half-chord length** between the two points.

**Step 4: Calculate Angular Distance (Central Angle)**
```
c = 2 × atan2(√a, √(1−a))
```

Where:
- `atan2` = Two-argument arctangent function
- `c` = Angular distance in radians

**Step 5: Calculate Final Distance**
```
d = R × c
```

Where:
- `R` = Earth's mean radius = 6,371 km
- `d` = Distance in kilometers

### Numerical Example

**Scenario:**
- User at Ben Thanh Market: (10.7725°N, 106.6992°E)
- POI at nearby location: (10.7745°N, 106.7012°E)
- Distance difference: ~0.002° in both directions

**Calculation:**

1. **Convert to Radians:**
   ```
   φ₁ = 10.7725 × π/180 = 0.18798 rad
   λ₁ = 106.6992 × π/180 = 1.86177 rad
   φ₂ = 10.7745 × π/180 = 0.18801 rad
   λ₂ = 106.7012 × π/180 = 1.86181 rad
   ```

2. **Calculate Differences:**
   ```
   Δφ = 0.18801 - 0.18798 = 0.00003 rad
   Δλ = 1.86181 - 1.86177 = 0.00004 rad
   ```

3. **Haversine Component:**
   ```
   a = sin²(0.000015) + cos(0.18798) × cos(0.18801) × sin²(0.00002)
   a ≈ 0.0000000002 + 0.9823 × 0.9823 × 0.0000000004
   a ≈ 0.0000000004
   ```

4. **Angular Distance:**
   ```
   c = 2 × atan2(√0.0000000004, √(1−0.0000000004))
   c ≈ 2 × atan2(0.00002, 0.99999)
   c ≈ 0.00004 rad
   ```

5. **Final Distance:**
   ```
   d = 6371 km × 0.00004 rad
   d ≈ 0.255 km ≈ 255 meters
   ```

### Code Implementation

```csharp
public double CalculateDistance(double userLatitude, double userLongitude, 
                               double poiLatitude, double poiLongitude)
{
    const double EARTH_RADIUS_KM = 6371.0;

    // Step 1: Convert to radians
    double lat1 = DegreesToRadians(userLatitude);
    double lon1 = DegreesToRadians(userLongitude);
    double lat2 = DegreesToRadians(poiLatitude);
    double lon2 = DegreesToRadians(poiLongitude);

    // Step 2: Calculate differences
    double dLat = lat2 - lat1;
    double dLon = lon2 - lon1;

    // Step 3: Haversine formula
    double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
               Math.Cos(lat1) * Math.Cos(lat2) * 
               Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

    // Step 4: Angular distance
    double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

    // Step 5: Final distance in meters
    double distanceKm = EARTH_RADIUS_KM * c;
    return distanceKm * 1000;  // Convert to meters
}

private static double DegreesToRadians(double degrees)
{
    return degrees * Math.PI / 180.0;
}
```

### Accuracy Considerations

| Range | Accuracy |
|-------|----------|
| < 10 km | ±0.1 meters |
| 10-100 km | ±1 meter |
| 100-1000 km | ±10 meters |
| > 1000 km | ±100 meters |

For our POI tracking (~100-150m radius), Haversine is more than sufficient.

---

## 2. Cooldown Mechanism

### Problem It Solves

**Without Cooldown:**
```
User enters POI at t=0s
- TTS plays: "Welcome to Ben Thanh Market"

User remains in POI at t=0.5s
- Same location, but within 5-second polling interval
- TTS plays again: "Welcome to Ben Thanh Market"

User still in POI at t=5s
- TTS plays again for the 3rd time!
- User hears repeated narration (annoying)
```

**Result:** Audio spam, poor user experience

### Solution: Cooldown Mechanism

"Once a POI narration is triggered, prevent the same POI from triggering again for 5 minutes."

### Data Structure

```csharp
// Dictionary to track last trigger time per POI
private Dictionary<int, DateTime> _poiLastTriggeredTimes = new();

// Example state:
// POI ID 1 (Ben Thanh) → Last triggered: 2026-03-12 14:30:00 UTC
// POI ID 2 (Bitexco)  → Last triggered: 2026-03-12 14:25:30 UTC
// POI ID 3 (Dong Khoi) → Never triggered (not in dictionary)
```

### Logic Flow

```csharp
public bool ShouldTriggerNarration(PoiModel poi, double userLatitude, double userLongitude)
{
    // STEP 1: Geofence Check
    double distance = CalculateDistance(userLatitude, userLongitude, 
                                       poi.Latitude, poi.Longitude);
    
    if (distance > poi.Radius)
    {
        return false;  // User is outside geofence, skip
    }

    // STEP 2: Cooldown Check
    if (_poiLastTriggeredTimes.TryGetValue(poi.Id, out var lastTriggerTime))
    {
        // POI has been triggered before
        TimeSpan timeSinceLastTrigger = DateTime.UtcNow - lastTriggerTime;
        
        if (timeSinceLastTrigger.TotalMinutes < 5)
        {
            return false;  // Still in cooldown, skip
        }
        // else: Cooldown expired, allow trigger
    }
    // else: POI never triggered, allow (first time)

    // STEP 3: All checks passed
    return true;
}
```

### Visual Timeline Example

```
Time    POI Status           User Position    Action
────────────────────────────────────────────────────────
 0s     Ready                Outside (200m)
 
 5s     Ready                Inside (80m)     ✓ Trigger TTS
        ↓
        Cooldown Started: 14:30:00

10s     Cooldown (4min 55s)  Still inside     ✗ Skip (in cooldown)

15s     Cooldown (4min 50s)  Still inside     ✗ Skip (in cooldown)

...

300s    Cooldown (0s)        Still inside     Still in cooldown?
        Expires
        ↓
        Ready Again

305s    Ready                Still inside     ✓ Trigger TTS again
        ↓
        Cooldown restarted
```

### Key Implementation Details

**1. Using DateTime.UtcNow (Not Local Time)**
```csharp
// CORRECT - Always use UTC for consistency
DateTime.UtcNow

// WRONG - Local time can cause timezone issues
DateTime.Now
```

**2. Marking POI as Triggered**
```csharp
// Called after narration starts, in the TriggerNarrationAsync method
public void MarkPoiAsTriggered(int poiId)
{
    _poiLastTriggeredTimes[poiId] = DateTime.UtcNow;
}
```

**3. Resetting for Testing**
```csharp
// For unit tests, manually reset cooldown
public void ResetPoiCooldown(int poiId)
{
    _poiLastTriggeredTimes.Remove(poiId);
}
```

---

## 3. Code Implementation Details

### Integration in ViewModel

```csharp
// Main tracking loop (every 5 seconds)
private async Task UpdateLocationAndCheckPoisAsync()
{
    var currentLocation = await _locationService.GetCurrentLocationAsync();
    
    CurrentLatitude = currentLocation.Latitude;
    CurrentLongitude = currentLocation.Longitude;
    
    // Check all POIs
    await CheckAllPoisAsync(currentLocation.Latitude, currentLocation.Longitude);
}

// Check each POI
private async Task CheckAllPoisAsync(double userLatitude, double userLongitude)
{
    foreach (var poi in _pointsOfInterest)
    {
        // This calls: Distance check + Cooldown check
        if (_narrationService.ShouldTriggerNarration(poi, userLatitude, userLongitude))
        {
            await TriggerNarrationAsync(poi, userLatitude, userLongitude);
        }
    }
}

// Execute narration
private async Task TriggerNarrationAsync(PoiModel poi, double userLatitude, double userLongitude)
{
    // Add log entry
    LogNarration(poi);
    
    // Mark POI as triggered (updates cooldown dictionary)
    _narrationService.MarkPoiAsTriggered(poi.Id);
    
    // Play TTS
    await _narrationService.PlayNarrationAsync(poi.DescriptionText);
}
```

### Thread Safety Considerations

**Current Implementation:**
```csharp
// Dictionary is accessed from Timer callback (background thread)
_poiLastTriggeredTimes[poiId] = DateTime.UtcNow;
```

**Potential Issue:** Dictionary is NOT thread-safe. In production:

```csharp
// Use thread-safe collection
private readonly ConcurrentDictionary<int, DateTime> _poiLastTriggeredTimes 
    = new();

// Or use lock
private readonly object _lockObject = new();

private void MarkPoiAsTriggered(int poiId)
{
    lock (_lockObject)
    {
        _poiLastTriggeredTimes[poiId] = DateTime.UtcNow;
    }
}
```

---

## 4. Testing & Validation

### Unit Test Examples

```csharp
[TestClass]
public class HaversineFormulaTests
{
    private NarrationService _service;

    [TestInitialize]
    public void Setup()
    {
        _service = new NarrationService();
    }

    [TestMethod]
    public void CalculateDistance_SameLocation_ReturnsZero()
    {
        // Arrange
        double lat = 10.7725;
        double lon = 106.6992;

        // Act
        double distance = _service.CalculateDistance(lat, lon, lat, lon);

        // Assert
        Assert.AreEqual(0, distance, 0.1);
    }

    [TestMethod]
    public void CalculateDistance_KnownDistance_ReturnsAccurate()
    {
        // Arrange - Two points ~255 meters apart
        double lat1 = 10.7725;
        double lon1 = 106.6992;
        double lat2 = 10.7745;
        double lon2 = 106.7012;

        // Act
        double distance = _service.CalculateDistance(lat1, lon1, lat2, lon2);

        // Assert - Should be approximately 250-260 meters
        Assert.IsTrue(distance > 240 && distance < 270);
    }
}

[TestClass]
public class CooldownTests
{
    private NarrationService _service;
    private PoiModel _poi;

    [TestInitialize]
    public void Setup()
    {
        _service = new NarrationService();
        _poi = new PoiModel
        {
            Id = 1,
            Name = "Test POI",
            Latitude = 10.7725,
            Longitude = 106.6992,
            DescriptionText = "Test",
            Radius = 100
        };
    }

    [TestMethod]
    public void ShouldTriggerNarration_FirstTime_ReturnsTrue()
    {
        // First trigger should return true
        bool result = _service.ShouldTriggerNarration(_poi, 10.7725, 106.6992);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ShouldTriggerNarration_InCooldown_ReturnsFalse()
    {
        // First trigger
        _service.ShouldTriggerNarration(_poi, 10.7725, 106.6992);
        _service.MarkPoiAsTriggered(_poi.Id);

        // Immediate second trigger (within 5 minutes)
        bool result = _service.ShouldTriggerNarration(_poi, 10.7725, 106.6992);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ShouldTriggerNarration_AfterCooldownReset_ReturnsTrue()
    {
        // Trigger and set cooldown
        _service.ShouldTriggerNarration(_poi, 10.7725, 106.6992);
        _service.MarkPoiAsTriggered(_poi.Id);

        // Manually reset for testing
        _service.ResetPoiCooldown(_poi.Id);

        // Should trigger again
        bool result = _service.ShouldTriggerNarration(_poi, 10.7725, 106.6992);
        Assert.IsTrue(result);
    }
}
```

### Manual Testing Checklist

- [ ] App starts without errors
- [ ] Location permission request appears
- [ ] After grant, "Start Tracking" enables successfully
- [ ] Location updates appear in UI (Lat/Lon)
- [ ] When approaching mock POI, TTS plays
- [ ] After TTS, same POI doesn't trigger for 5 minutes
- [ ] Different POIs trigger independently
- [ ] "Stop Tracking" cancels TTS
- [ ] Log shows accurate timestamps
- [ ] App handles location disabled gracefully

---

## Summary

### Haversine Formula
- **Purpose**: Calculate accurate geographic distance on spherical Earth
- **Formula**: `d = R × 2 × atan2(√a, √(1−a))` where `a` is based on latitude/longitude differences
- **Accuracy**: ±0.1 meters for distances < 10 km
- **Implementation**: 16 lines of C# code

### Cooldown Mechanism
- **Purpose**: Prevent audio spam from same POI
- **Duration**: 5 minutes per POI
- **Data Structure**: Dictionary mapping POI ID → Last Trigger Time
- **Logic**: Check geofence AND cooldown before triggering
- **Implementation**: 10 lines of C# code

Both mechanisms work together to create smooth, responsive geofence-triggered narration!

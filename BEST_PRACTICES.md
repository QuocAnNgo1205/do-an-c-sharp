# NetTopologySuite Best Practices for Your API

## 1. Always Use SpatialHelper for Point Creation

### ✅ DO
```csharp
var location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);
poi.Location = location;
```

### ❌ DON'T
```csharp
var factory = new GeometryFactory(new PrecisionModel(), 4326);
poi.Location = factory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));
// Duplicates GeometryFactory, violates DRY principle
```

---

## 2. Always Extract Coordinates in Select Statements

### ✅ DO
```csharp
.Select(p => new PoiDto
{
    Id = p.Id,
    Name = p.Name,
    Latitude = SpatialHelper.GetLatitude(p.Location),
    Longitude = SpatialHelper.GetLongitude(p.Location),
    // ...
})
```

### ❌ DON'T
```csharp
.Select(p => new PoiDto
{
    Id = p.Id,
    Name = p.Name,
    Latitude = p.Location.Coordinate.Y,      // Direct access to internal structure
    Longitude = p.Location.Coordinate.X,
    // ...
})
```

---

## 3. Use Spatial Queries at Database Level

### ✅ DO (Optimal Performance)
```csharp
var userLocation = SpatialHelper.CreatePoint(userLat, userLng);
var nearbyPois = await _context.Pois
    .Where(p => p.Status == PoiStatus.Approved && 
           p.Location.Distance(userLocation) <= radiusInMeters)
    .Select(...)
    .ToListAsync();
```

### ⚠️ AVOID (Poor Performance)
```csharp
var approvedPois = await _context.Pois
    .Where(p => p.Status == PoiStatus.Approved)
    .Select(p => new { p.Id, p.Location })
    .ToListAsync();

var nearbyPois = approvedPois
    .Where(p => p.Location.Distance(userLocation) <= radiusInMeters)
    .ToList();

// This loads ALL approved POIs from database, then filters in memory!
```

---

## 4. Input Validation for Coordinates

### ✅ DO: Validate Coordinate Ranges
```csharp
[HttpPost]
public async Task<IActionResult> CreateNewPoi([FromBody] CreatePoiRequest request)
{
    // Validate latitude: -90 to 90
    if (request.Latitude < -90 || request.Latitude > 90)
    {
        return BadRequest(new { Message = "Latitude must be between -90 and 90" });
    }
    
    // Validate longitude: -180 to 180
    if (request.Longitude < -180 || request.Longitude > 180)
    {
        return BadRequest(new { Message = "Longitude must be between -180 and 180" });
    }
    
    // Validate radius
    if (radiusInMeters <= 0)
    {
        return BadRequest(new { Message = "Radius must be greater than 0" });
    }
    
    // ... proceed with creation
}
```

### ❌ DON'T: Skip Validation
```csharp
// No validation - bad coordinates could be stored
var location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);
```

---

## 5. Handle Null Points Gracefully

### ✅ DO: Check for Null
```csharp
if (poi.Location == null)
{
    return BadRequest(new { Message = "POI location is not set" });
}

double lat = SpatialHelper.GetLatitude(poi.Location);  // Safe
```

### ❌ DON'T: Ignore Null
```csharp
double lat = SpatialHelper.GetLatitude(poi.Location);  // May throw if null
```

---

## 6. Use Consistent SRID Across Application

### ✅ DO: Always SRID 4326
```csharp
// All Points created with SpatialHelper use SRID 4326
var point1 = SpatialHelper.CreatePoint(10.8, 106.7);      // SRID 4326
var point2 = SpatialHelper.CreatePoint(10.9, 106.8);      // SRID 4326
var distance = point1.Distance(point2);                    // Works!
```

### ❌ DON'T: Mix Different SRIDs
```csharp
var factory1 = new GeometryFactory(new PrecisionModel(), 4326);
var factory2 = new GeometryFactory(new PrecisionModel(), 3857);
var point1 = factory1.CreatePoint(new Coordinate(106.7, 10.8));
var point2 = factory2.CreatePoint(new Coordinate(106.8, 10.9));
var distance = point1.Distance(point2);  // ERROR: SRID mismatch!
```

---

## 7. Performance: Spatial Indexing

### ✅ DO: Create Spatial Index for Production
```sql
-- One-time setup
CREATE SPATIAL INDEX IX_Pois_Location 
ON Pois(Location)
USING GEOGRAPHY_GRID
WITH (
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16,
    PAD_INDEX = ON
);
```

### 📊 Expected Impact
- Queries on 100 POIs: ~10ms → ~5ms
- Queries on 10,000 POIs: ~500ms → ~20ms
- Queries on 100,000 POIs: ~5000ms → ~50ms

---

## 8. Testing Spatial Queries

### ✅ DO: Unit Test Examples
```csharp
[TestClass]
public class SpatialHelperTests
{
    [TestMethod]
    public void CreatePoint_WithValidCoordinates_ReturnsPoint()
    {
        // Arrange
        double lat = 10.798955;
        double lng = 106.70090;
        
        // Act
        var point = SpatialHelper.CreatePoint(lat, lng);
        
        // Assert
        Assert.IsNotNull(point);
        Assert.AreEqual(lat, SpatialHelper.GetLatitude(point));
        Assert.AreEqual(lng, SpatialHelper.GetLongitude(point));
    }
    
    [TestMethod]
    public void GetLatitude_WithNullPoint_Returns0()
    {
        // Act
        var lat = SpatialHelper.GetLatitude(null);
        
        // Assert
        Assert.AreEqual(0.0, lat);
    }
}
```

---

## 9. API Documentation

### ✅ DO: Document Coordinate Format
```csharp
/// <summary>
/// Get nearby POIs within a specified radius.
/// </summary>
/// <param name="userLat">User's latitude (WGS84, SRID 4326)</param>
/// <param name="userLng">User's longitude (WGS84, SRID 4326)</param>
/// <param name="radiusInMeters">Search radius in meters (default: 50m)</param>
/// <returns>List of POIs within the specified radius</returns>
[HttpGet("public/nearby")]
public async Task<IActionResult> GetNearbyPois(
    [FromQuery] double userLat, 
    [FromQuery] double userLng, 
    [FromQuery] double radiusInMeters = 50)
{
    // Implementation
}
```

---

## 10. Migration Best Practices

### ✅ DO: Test Migration on Copy of Database
```powershell
# 1. Backup production database
# 2. Restore to development/staging database
# 3. Test migration on copy first

dotnet ef migrations add AddSpatialLocationToPoiReplaceLatLng
dotnet ef database update  # Test on staging first

# 4. Only then apply to production
```

### ⚠️ DO: Include Data Migration in Migration Script
If you have existing data, the migration should:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. Add new column with default
    migrationBuilder.AddColumn<Point>(
        name: "Location",
        table: "Pois",
        type: "geography",
        defaultValueSql: "geography::Point(0, 0, 4326)");
    
    // 2. Populate from old columns (if they exist)
    migrationBuilder.Sql(@"
        UPDATE Pois 
        SET Location = geography::Point(Longitude, Latitude, 4326) 
        WHERE Latitude IS NOT NULL AND Longitude IS NOT NULL");
    
    // 3. Drop old columns
    migrationBuilder.DropColumn("Latitude", "Pois");
    migrationBuilder.DropColumn("Longitude", "Pois");
}
```

---

## 11. Error Handling

### ✅ DO: Catch Spatial Exceptions
```csharp
try
{
    var distance = point1.Distance(point2);
}
catch (ArgumentNullException ex)
{
    return BadRequest(new { Message = "Invalid point coordinates", Error = ex.Message });
}
catch (InvalidOperationException ex)
{
    return BadRequest(new { Message = "Spatial operation failed", Error = ex.Message });
}
```

### ❌ DON'T: Let Exceptions Bubble Up
```csharp
var distance = point1.Distance(point2);  // May throw, will result in 500 error
```

---

## 12. Logging Spatial Operations

### ✅ DO: Log Coordinate Operations
```csharp
public async Task<IActionResult> GetNearbyPois(double userLat, double userLng, double radiusInMeters)
{
    _logger.LogInformation($"GetNearbyPois: userLat={userLat}, userLng={userLng}, radius={radiusInMeters}m");
    
    var userLocation = SpatialHelper.CreatePoint(userLat, userLng);
    _logger.LogDebug($"User location Point created: {userLocation.AsText()}");
    
    var nearbyPois = await _context.Pois
        .Where(p => p.Status == PoiStatus.Approved && 
               p.Location.Distance(userLocation) <= radiusInMeters)
        .ToListAsync();
    
    _logger.LogInformation($"Found {nearbyPois.Count} POIs within {radiusInMeters}m");
    return Ok(nearbyPois);
}
```

---

## 13. DTOs Should Mirror Database Layer

### ✅ DO: Separate Request/Response DTOs
```csharp
// Request DTO - what client sends
public class CreatePoiRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

// Response DTO - what client receives
public class PoiDto
{
    public double Latitude { get; set; }  // Extracted from Point
    public double Longitude { get; set; }  // Extracted from Point
}
```

---

## 14. Query Optimization Tips

### Combine Filters for Better Performance
```csharp
// ✅ GOOD: Multiple filters at database level
var nearbyApprovedOwnedByUser = await _context.Pois
    .Where(p => 
        p.Status == PoiStatus.Approved &&           // Filter by status
        p.OwnerId == userId &&                       // Filter by owner
        p.Location.Distance(userLocation) <= 5000)  // Filter by distance
    .ToListAsync();
// All filtering happens in SQL!

// ❌ AVOID: Filtering in memory
var allApprovedPois = await _context.Pois
    .Where(p => p.Status == PoiStatus.Approved)
    .ToListAsync();

var userPois = allApprovedPois
    .Where(p => p.OwnerId == userId)
    .ToList();

var nearbyPois = userPois
    .Where(p => p.Location.Distance(userLocation) <= 5000)
    .ToList();
// Loads all approved POIs, then filters in memory - wasteful!
```

---

## 15. Monitoring and Performance

### Key Metrics to Monitor
1. **Query Execution Time**: Track GetNearbyPois performance
2. **Data Transfer**: Monitor network payload size
3. **Index Fragmentation**: Regular maintenance of spatial indexes
4. **Cache Hit Rate**: If implementing caching

### SQL Query Plan Analysis
```sql
-- Enable actual execution plan
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Run your query
SELECT * FROM Pois 
WHERE Status = 'Approved' 
  AND Location.STDistance(@userLocation) <= 5000;

-- Check for:
-- 1. Spatial index usage
-- 2. Table scan vs index seek
-- 3. I/O cost
```

---

## Summary

| Practice | Impact | Priority |
|----------|--------|----------|
| Use SpatialHelper consistently | Maintainability | HIGH |
| Database-level spatial queries | Performance | HIGH |
| Input coordinate validation | Reliability | HIGH |
| Spatial indexing | Scalability | MEDIUM |
| Error handling | Stability | MEDIUM |
| API documentation | Usability | MEDIUM |
| Migration testing | Safety | HIGH |
| Logging | Debugging | MEDIUM |

---

## Resources

- [NetTopologySuite GitHub](https://github.com/NetTopologySuite/NetTopologySuite)
- [SQL Server Spatial Data](https://learn.microsoft.com/en-us/sql/relational-databases/spatial/spatial-data-sql-server)
- [Entity Framework Core Spatial Data](https://learn.microsoft.com/en-us/ef/core/modeling/spatial)
- [WGS84 (SRID 4326) Reference](https://epsg.io/4326)


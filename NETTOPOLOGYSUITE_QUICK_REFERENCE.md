# NetTopologySuite Quick Reference

## 1. NuGet Package Installation (One-Time)

```powershell
# Run in your API project directory
dotnet add package NetTopologySuite --version 2.5.1
dotnet add package NetTopologySuite.IO.SqlServerBytes --version 2.5.1
dotnet restore
```

---

## 2. Program.cs Configuration

```csharp
using NetTopologySuite.Geometries;

builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            x => x.UseNetTopologySuite()  // <-- Add this line
        )
);
```

---

## 3. Entity Model Example

```csharp
using NetTopologySuite.Geometries;

public class Poi
{
    public int Id { get; set; }
    [Required]
    public Point Location { get; set; } = null!;  // Instead of Latitude + Longitude
    // ... other properties
}
```

---

## 4. DbContext Configuration

```csharp
modelBuilder.Entity<Poi>(entity =>
{
    entity.HasKey(p => p.Id);
    entity.Property(p => p.Location)
        .IsRequired()
        .HasColumnType("geography");  // Use geography for Earth-based coordinates
});
```

---

## 5. Spatial Helper Methods

```csharp
// Create a Point from latitude/longitude
Point location = SpatialHelper.CreatePoint(latitude: 10.798955, longitude: 106.7009);

// Extract latitude from Point
double lat = SpatialHelper.GetLatitude(point);

// Extract longitude from Point
double lng = SpatialHelper.GetLongitude(point);
```

---

## 6. Distance Query Example (Key Pattern)

```csharp
// Create user's location Point
var userLocation = SpatialHelper.CreatePoint(userLat, userLng);

// Query POIs within radius (database-level filtering)
var nearbyPois = await _context.Pois
    .Where(p => p.Status == PoiStatus.Approved && 
           p.Location.Distance(userLocation) <= radiusInMeters)  // Distance in meters
    .Select(p => new PoiDto
    {
        Id = p.Id,
        Name = p.Name,
        Latitude = SpatialHelper.GetLatitude(p.Location),
        Longitude = SpatialHelper.GetLongitude(p.Location),
        Status = p.Status,
        Translations = p.Translations.Select(t => new PoiTranslationDto { ... }).ToList()
    })
    .ToListAsync();
```

---

## 7. Creating a New POI

### Request (from client)
```json
{
  "name": "Quán mới",
  "latitude": 10.800,
  "longitude": 106.702,
  "title": "Nhà hàng sushi",
  "description": "Quán ăn Nhật Bản"
}
```

### Controller Code
```csharp
[HttpPost]
public async Task<IActionResult> CreateNewPoi([FromBody] CreatePoiRequest request)
{
    // Convert DTO coordinates to Point
    var location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);

    var newPoi = new Poi
    {
        OwnerId = ownerId,
        Name = request.Name,
        Location = location,  // Store as Point
        Status = PoiStatus.Pending,
        Translations = new List<PoiTranslation> { ... }
    };

    _context.Pois.Add(newPoi);
    await _context.SaveChangesAsync();
    return Ok(new { Message = "POI created", Id = newPoi.Id });
}
```

### Response (to client)
```json
{
  "id": 1,
  "name": "Quán mới",
  "latitude": 10.800,
  "longitude": 106.702,
  "status": "Pending"
}
```

---

## 8. Database Migration

```powershell
# Create migration
dotnet ef migrations add AddSpatialLocationToPoiReplaceLatLng

# Review the generated migration file

# Apply migration
dotnet ef database update
```

---

## 9. SQL Server Spatial Index (Optional, Recommended for Production)

```sql
CREATE SPATIAL INDEX IX_Pois_Location 
ON Pois(Location)
USING GEOGRAPHY_GRID
WITH (
    GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    CELLS_PER_OBJECT = 16,
    PAD_INDEX = ON
);
```

---

## 10. Key Points to Remember

| Aspect | Detail |
|--------|--------|
| **SRID** | 4326 (WGS84 - standard for GPS) |
| **Coordinate Order** | (Longitude, Latitude) in geometry; X = Longitude, Y = Latitude |
| **Column Type** | `geography` (for Earth-based) not `geometry` (for flat planes) |
| **Distance Units** | Meters (for geography on SRID 4326) |
| **API Response** | Still use Latitude/Longitude in JSON for client compatibility |
| **Database Query** | Use `Location.Distance(point)` for spatial filtering |

---

## 11. Common Methods

```csharp
// Distance between two points (in meters)
double distance = point1.Distance(point2);

// Check if point is within polygon
bool isInside = polygon.Contains(point);

// Get buffer around point (circle)
var buffer = point.Buffer(radiusInMeters);

// Calculate centroid of geometry
Point centroid = geometry.Centroid;
```

---

## 12. Troubleshooting

| Problem | Solution |
|---------|----------|
| **"The point is not valid"** | Ensure coordinates are in (Longitude, Latitude) order |
| **"SRID mismatch"** | Use SpatialHelper.CreatePoint() to ensure consistent SRID 4326 |
| **Null reference errors** | Check SpatialHelper methods handle null points |
| **Poor query performance** | Add spatial index on Location column |
| **Distance always 0** | Verify both points have same SRID (4326) |

---

## 13. Files Modified/Created

- ✅ `Program.cs` - UseNetTopologySuite()
- ✅ `Models/PoiStatus.cs` - Location Point property
- ✅ `Data/AppDbContext.cs` - HasColumnType("geography")
- ✅ `DTOs/PoiDto.cs` - Updated to use SpatialHelper
- ✅ `DTOs/CreatePoiRequest.cs` - Request DTO
- ✅ `Controller/PoiController.cs` - Updated all endpoints
- ✅ `SpatialHelper.cs` - NEW spatial utility class
- ✅ `VinhKhanhFoodTour.API.csproj` - Added NuGet packages


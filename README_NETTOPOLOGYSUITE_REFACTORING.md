# NetTopologySuite Refactoring Guide

## Overview
This document provides a complete guide for refactoring your ASP.NET Core API to use NetTopologySuite (NTS) for spatial queries. The refactoring replaces basic Latitude/Longitude fields with geometry-based Point objects and moves distance calculations to the database level for optimal performance.

---

## Step 1: NuGet Package Installation

### Command
Run these commands in your API project directory:

```powershell
dotnet add package NetTopologySuite --version 2.5.1
dotnet add package NetTopologySuite.IO.SqlServerBytes --version 2.5.1
```

### What These Packages Do
- **NetTopologySuite**: Provides geometry primitives (Point, LineString, Polygon, etc.) and spatial operations
- **NetTopologySuite.IO.SqlServerBytes**: Enables serialization/deserialization of geometry objects for SQL Server's `geography` type

### Package Configuration in .csproj
The following has been added to your `VinhKhanhFoodTour.API.csproj`:

```xml
<PackageReference Include="NetTopologySuite" Version="2.5.1" />
<PackageReference Include="NetTopologySuite.IO.SqlServerBytes" Version="2.5.1" />
```

---

## Step 2: Configure UseNetTopologySuite()

### Location: Program.cs

Your DbContext configuration now includes spatial support:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            x => x.UseNetTopologySuite()
        )
);
```

**Key Points:**
- `UseNetTopologySuite()` enables EF Core to work with geometry types
- This must be called on the SQL Server provider configuration
- The import statement is: `using NetTopologySuite.Geometries;`

---

## Step 3: Entity Model Refactoring

### Before (Old Model)
```csharp
public class Poi
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public double Latitude { get; set; }
    
    [Required]
    public double Longitude { get; set; }
    
    public double TriggerRadius { get; set; } = 20.0;
    public PoiStatus Status { get; set; } = PoiStatus.Pending;
    // ... other properties
}
```

### After (New Model)
```csharp
using NetTopologySuite.Geometries;

public class Poi
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public Point Location { get; set; } = null!;
    
    public double TriggerRadius { get; set; } = 20.0;
    public PoiStatus Status { get; set; } = PoiStatus.Pending;
    // ... other properties
}
```

**Changes:**
- Replaced `Latitude` and `Longitude` (double) with `Location` (Point)
- The Point object is now the single source of truth for spatial coordinates
- SRID 4326 (WGS84) is set at configuration, not on the property

---

## Step 4: DbContext Configuration

### Location: Data/AppDbContext.cs

The Poi entity configuration now defines the spatial column:

```csharp
modelBuilder.Entity<Poi>(entity =>
{
    entity.HasKey(p => p.Id);
    entity.Property(p => p.Name).IsRequired().HasMaxLength(255);
    
    // Configure spatial property with SRID 4326 (WGS84 - latitude/longitude)
    entity.Property(p => p.Location)
        .IsRequired()
        .HasColumnType("geography");
    
    entity.Property(p => p.TriggerRadius).HasDefaultValue(20.0);
    entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    
    // ... other configurations
});
```

**Key Points:**
- `HasColumnType("geography")` tells SQL Server to use the SQL geography type (not the geometry type)
- Geography is appropriate for Earth-based coordinates (SRID 4326)
- Distance calculations on geography columns return results in meters

---

## Step 5: DTO Refactoring

### PoiDto (Response DTO)
DTOs still expose Latitude and Longitude for API clients:

```csharp
public class PoiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Latitude extracted from Location Point for API response
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude extracted from Location Point for API response
    /// </summary>
    public double Longitude { get; set; }
    
    public PoiStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public List<PoiTranslationDto> Translations { get; set; } = new();
}
```

### CreatePoiRequest (Request DTO)
Clients still send Latitude and Longitude; conversion happens in the controller:

```csharp
public class CreatePoiRequest
{
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Latitude in WGS84 (SRID 4326)
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude in WGS84 (SRID 4326)
    /// </summary>
    public double Longitude { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

---

## Step 6: Spatial Helper Class

### Location: SpatialHelper.cs

This utility class encapsulates all spatial conversions:

```csharp
using NetTopologySuite.Geometries;

public static class SpatialHelper
{
    private const int SRID_WGS84 = 4326;
    private static readonly GeometryFactory GeometryFactory = 
        new GeometryFactory(new PrecisionModel(), SRID_WGS84);

    /// <summary>
    /// Create a Point geometry from latitude and longitude coordinates.
    /// </summary>
    public static Point CreatePoint(double latitude, double longitude)
    {
        // Note: In geography, the order is (longitude, latitude)
        return GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
    }

    /// <summary>
    /// Extract latitude from a Point geometry.
    /// </summary>
    public static double GetLatitude(Point point)
    {
        return point?.Coordinate?.Y ?? 0.0;
    }

    /// <summary>
    /// Extract longitude from a Point geometry.
    /// </summary>
    public static double GetLongitude(Point point)
    {
        return point?.Coordinate?.X ?? 0.0;
    }
}
```

**Important Notes:**
- **Coordinate Order**: In geography, coordinates are (longitude, latitude), but the Y component is latitude and X is longitude
- **SRID 4326**: WGS84 (World Geodetic System 1984) - the standard for GPS coordinates
- **GeometryFactory**: Ensures all points use consistent SRID across the application

---

## Step 7: Updated API Endpoints

### CreateNewPoi Endpoint

**Before:**
```csharp
var newPoi = new Poi
{
    OwnerId = ownerId,
    Name = request.Name,
    Latitude = request.Latitude,
    Longitude = request.Longitude,
    Status = PoiStatus.Pending,
    // ...
};
```

**After:**
```csharp
// Convert DTO coordinates to Point
var location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);

var newPoi = new Poi
{
    OwnerId = ownerId,
    Name = request.Name,
    Location = location,
    Status = PoiStatus.Pending,
    // ...
};
```

### GetNearbyPois Endpoint (Key Change)

**Before (In-Memory Calculation):**
```csharp
var approvedPois = await _context.Pois
    .Where(p => p.Status == PoiStatus.Approved)
    .Select(p => new PoiDto { ... })
    .ToListAsync();

var nearbyPois = approvedPois
    .Where(p => CalculateDistance(userLat, userLng, p.Latitude, p.Longitude) <= radiusInMeters)
    .ToList();
```

**After (Database-Level Spatial Query):**
```csharp
var userLocation = SpatialHelper.CreatePoint(userLat, userLng);

var nearbyPois = await _context.Pois
    .Where(p => p.Status == PoiStatus.Approved && 
           p.Location.Distance(userLocation) <= radiusInMeters)
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

**Benefits:**
- **Database-level filtering**: Only POIs within the radius are fetched from the database
- **Better performance**: Large datasets are filtered before network transmission
- **Scalability**: Spatial indexes on the Location column further optimize queries
- **Distance in meters**: SQL Server's STDistance() on geography type returns meters

---

## Step 8: Key Implementation Details

### DTO to Entity Mapping in Controllers

All controller methods that create or update POIs now convert coordinates:

```csharp
// In CreatePoi, UpdatePoi, CreateNewPoi methods:
var location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);
poi.Location = location;
```

### Entity to DTO Mapping in Select Queries

All LINQ Select clauses now extract coordinates from the Point:

```csharp
.Select(p => new PoiDto
{
    Id = p.Id,
    Name = p.Name,
    Latitude = SpatialHelper.GetLatitude(p.Location),
    Longitude = SpatialHelper.GetLongitude(p.Location),
    Status = p.Status,
    // ...
})
```

---

## Database Migration Steps

### Step 1: Create Migration
```powershell
cd backend/VinhKhanhFoodTour.API
dotnet ef migrations add AddSpatialLocationToPoiReplaceLatLng
```

### Step 2: Review Migration
The migration file should:
1. Drop the existing Latitude and Longitude columns
2. Add a new Location column of type `geography`
3. Populate Location from old Latitude/Longitude (if existing data exists)

### Step 3: Apply Migration
```powershell
dotnet ef database update
```

### SQL Example (for Reference)
```sql
-- If you need manual migration guidance:
-- Add the geography column
ALTER TABLE Pois ADD Location geography NOT NULL DEFAULT geography::Point(0, 0, 4326);

-- Populate from existing coordinates (example)
-- UPDATE Pois SET Location = geography::Point(Longitude, Latitude, 4326) WHERE Location IS NULL;

-- Drop old columns
ALTER TABLE Pois DROP COLUMN Latitude, Longitude;
```

---

## Performance Optimization: Spatial Index

### Creating a Spatial Index (Optional but Recommended)

For large datasets (thousands of POIs), create a spatial index:

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

This dramatically speeds up distance queries like the `/nearby` endpoint.

---

## Testing the Refactored API

### Test GetNearbyPois
```
GET /api/v1/poi/public/nearby?userLat=10.798955&userLng=106.7009&radiusInMeters=1000
```

**Expected Response:**
```json
[
  {
    "id": 1,
    "name": "Nhà hàng A",
    "latitude": 10.799,
    "longitude": 106.701,
    "status": "Approved",
    "translations": [...]
  }
]
```

### Test CreateNewPoi
```
POST /api/v1/poi
Content-Type: application/json

{
  "name": "Quán mới",
  "latitude": 10.800,
  "longitude": 106.702,
  "title": "Nhà hàng sushi",
  "description": "Quán ăn Nhật Bản"
}
```

---

## Common Pitfalls and FAQs

### Q: Why is the coordinate order (longitude, latitude)?
**A:** In the WGS84 geographic coordinate system, coordinates are represented as (longitude, latitude). This is because longitude comes first in the ISO 6709 standard. However, GIS often uses (latitude, longitude), so always be careful!

### Q: How do I migrate existing data?
**A:** You'll need to write a migration that:
1. Adds the Location column with a default
2. Populates it from existing Latitude/Longitude
3. Drops the old columns

EF Core can help generate this, but manual adjustments may be needed.

### Q: What if the coordinates are null?
**A:** The SpatialHelper methods return 0.0 for null points: `point?.Coordinate?.Y ?? 0.0`

### Q: Can I still query by latitude/longitude?
**A:** Yes, but extract them from the Point first:
```csharp
.Where(p => SpatialHelper.GetLatitude(p.Location) > 10.0)
```

However, this is inefficient for distance queries. Use spatial operations instead.

### Q: What about performance?
**A:** For distance queries, database-level spatial operations are significantly faster than in-memory calculations, especially with large datasets. The spatial index further accelerates queries.

---

## Summary of Files Modified

1. **Program.cs** - Added UseNetTopologySuite() configuration
2. **Models/PoiStatus.cs** - Replaced Latitude/Longitude with Location (Point)
3. **Data/AppDbContext.cs** - Configured spatial column as geography type
4. **DTOs/PoiDto.cs** - Updated to extract coordinates from Point
5. **DTOs/CreatePoiRequest.cs** - Created new DTO for consistency
6. **Controller/PoiController.cs** - Updated all methods to use SpatialHelper
7. **SpatialHelper.cs** - New utility class for spatial conversions
8. **VinhKhanhFoodTour.API.csproj** - Added NuGet packages

---

## Next Steps

1. **Install packages**: Run `dotnet restore`
2. **Create migration**: `dotnet ef migrations add [MigrationName]`
3. **Review migration file**: Ensure it handles existing data correctly
4. **Apply migration**: `dotnet ef database update`
5. **Test endpoints**: Verify all endpoints work with spatial data
6. **Add spatial index**: Optional but recommended for production


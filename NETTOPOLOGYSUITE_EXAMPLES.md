# NetTopologySuite Implementation Examples

## Example 1: Creating a New POI (POST Request)

### Client Request
```json
POST /api/v1/poi HTTP/1.1
Host: localhost:5000
Content-Type: application/json

{
  "name": "Nhà hàng Phở 88",
  "latitude": 10.798955,
  "longitude": 106.70090,
  "title": "Phở tại Sài Gòn",
  "description": "Quán phở nổi tiếng với nước dùng đặc biệt"
}
```

### Controller Code
```csharp
[HttpPost]
public async Task<IActionResult> CreateNewPoi([FromBody] CreatePoiRequest request)
{
    try
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var ownerId))
        {
            return Unauthorized(new { Message = "Không thể xác định người dùng từ token." });
        }

        // STEP 1: Convert DTO coordinates to Point (SRID 4326)
        var location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);

        // STEP 2: Create POI entity with Point
        var newPoi = new Poi
        {
            OwnerId = ownerId,
            Name = request.Name,
            Location = location,  // <-- Store as Point, not lat/lng
            Status = PoiStatus.Pending,
            TriggerRadius = 50,
            LastUpdated = DateTime.UtcNow,
            Translations = new List<PoiTranslation>
            {
                new PoiTranslation
                {
                    LanguageCode = "vi",
                    Title = request.Title,
                    Description = request.Description
                }
            }
        };

        // STEP 3: Save to database
        _context.Pois.Add(newPoi);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Đã thêm quán thành công! Chờ Admin duyệt nhé ông chủ.",
            Id = newPoi.Id
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new { Message = "Lỗi rồi đại vương ơi: " + ex.Message });
    }
}
```

### What Happens Internally
1. **Input**: Client sends Latitude=10.798955, Longitude=106.70090
2. **Conversion**: `SpatialHelper.CreatePoint()` creates a Point with:
   - X (Longitude) = 106.70090
   - Y (Latitude) = 10.798955
   - SRID = 4326 (WGS84)
3. **Storage**: Point is stored in database as `geography` type
4. **Output**: API returns `{ Message: "...", Id: 1 }`

### Database Storage
The Point is stored as SQL Server geography type:
```
Location: POINT (106.70090 10.798955)  [SRID: 4326]
```

---

## Example 2: Getting Nearby POIs (GET Request)

### Client Request
```
GET /api/v1/poi/public/nearby?userLat=10.800&userLng=106.701&radiusInMeters=2000
Host: localhost:5000
```

### Controller Code
```csharp
[HttpGet("public/nearby")]
[AllowAnonymous]
public async Task<IActionResult> GetNearbyPois(
    [FromQuery] double userLat, 
    [FromQuery] double userLng, 
    [FromQuery] double radiusInMeters = 50)
{
    try
    {
        // STEP 1: Create Point from user's coordinates
        var userLocation = SpatialHelper.CreatePoint(userLat, userLng);

        // STEP 2: Query using spatial distance at DATABASE LEVEL
        // This is the KEY optimization - distance filtering happens in SQL, not in C#
        var nearbyPois = await _context.Pois
            .Where(p => 
                p.Status == PoiStatus.Approved && 
                p.Location.Distance(userLocation) <= radiusInMeters)  // Distance in meters!
            .Select(p => new PoiDto
            {
                Id = p.Id,
                Name = p.Name,
                // STEP 3: Extract coordinates from Point for DTO
                Latitude = SpatialHelper.GetLatitude(p.Location),
                Longitude = SpatialHelper.GetLongitude(p.Location),
                Status = p.Status,
                RejectionReason = p.RejectionReason,
                Translations = p.Translations.Select(t => new PoiTranslationDto
                {
                    Id = t.Id,
                    LanguageCode = t.LanguageCode,
                    Title = t.Title,
                    Description = t.Description
                }).ToList()
            })
            .ToListAsync();

        return Ok(nearbyPois);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { Message = "Lỗi khi lấy danh sách quán gần đây: " + ex.Message });
    }
}
```

### SQL Generated (Approximate)
```sql
SELECT 
    p.[Id], p.[Name], p.[Location], p.[Status], p.[RejectionReason],
    t.[Id], t.[LanguageCode], t.[Title], t.[Description]
FROM [Pois] p
INNER JOIN [PoiTranslations] t ON p.[Id] = t.[PoiId]
WHERE 
    p.[Status] = 'Approved' AND 
    p.[Location].STDistance(@userLocation) <= 2000  -- Distance in meters
```

### Server-Side Processing
1. **User sends**: userLat=10.800, userLng=106.701, radiusInMeters=2000
2. **Convert**: Create userLocation Point (106.701, 10.800) with SRID 4326
3. **Query DB**: Only fetch POIs within 2000 meters
4. **Extract coords**: Convert Point back to Latitude/Longitude in DTO
5. **Return**: JSON with Latitude/Longitude

### Client Response
```json
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "id": 1,
    "name": "Nhà hàng Phở 88",
    "latitude": 10.798955,
    "longitude": 106.70090,
    "status": "Approved",
    "rejectionReason": null,
    "translations": [
      {
        "id": 1,
        "languageCode": "vi",
        "title": "Phở tại Sài Gòn",
        "description": "Quán phở nổi tiếng với nước dùng đặc biệt"
      }
    ]
  },
  {
    "id": 2,
    "name": "Nhà hàng Cơm Tấm",
    "latitude": 10.801234,
    "longitude": 106.702345,
    "status": "Approved",
    "translations": [...]
  }
]
```

### Performance Comparison

| Approach | Data Processing | Network Transfer |
|----------|-----------------|-----------------|
| **Old (Haversine)** | Load all POIs → Calculate distance in C# → Filter → Return | Transfers ALL POIs from DB |
| **New (NetTopology)** | Filter at DB level → Return only nearby POIs | Transfers only nearby POIs |

**Example with 10,000 POIs and 2km radius:**
- Old: Fetch 10,000 POIs from DB, filter in memory = larger network + slower
- New: Query DB with spatial index, fetch ~20-50 matching POIs = minimal network + faster

---

## Example 3: Updating a POI

### Client Request
```json
PUT /api/v1/poi/owner/1 HTTP/1.1
Host: localhost:5000
Content-Type: application/json
Authorization: Bearer {jwt_token}

{
  "name": "Nhà hàng Phở 88 - Mới",
  "latitude": 10.799000,
  "longitude": 106.701000
}
```

### Controller Code
```csharp
[HttpPut("owner/{id}")]
[Authorize(Roles = "Owner")]
public async Task<IActionResult> UpdatePoi(int id, [FromBody] UpdatePoiDto request)
{
    try
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var currentUserId))
        {
            return Unauthorized(new { Message = "Không thể xác định người dùng." });
        }

        var poi = await _context.Pois.FindAsync(id);
        if (poi == null)
            return NotFound(new { Message = $"Không tìm thấy quán với ID: {id}" });

        if (poi.OwnerId != currentUserId)
            return Forbid();

        // Update properties
        poi.Name = request.Name;
        // IMPORTANT: Convert new coordinates to Point
        poi.Location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);
        poi.Status = PoiStatus.Pending;  // Reset to pending after update
        poi.RejectionReason = null;
        poi.LastUpdated = DateTime.UtcNow;

        _context.Pois.Update(poi);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Đã cập nhật quán ăn thành công! Chờ Admin duyệt lại.", Id = poi.Id });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { Message = "Lỗi khi cập nhật quán: " + ex.Message });
    }
}
```

---

## Example 4: Returning POIs in API Response

### Selecting from Database
```csharp
var pois = await _context.Pois
    .Where(p => p.Status == PoiStatus.Approved)
    .Select(p => new PoiDto
    {
        Id = p.Id,
        Name = p.Name,
        // Extract coordinates from Point using SpatialHelper
        Latitude = SpatialHelper.GetLatitude(p.Location),
        Longitude = SpatialHelper.GetLongitude(p.Location),
        Status = p.Status,
        RejectionReason = p.RejectionReason,
        Translations = p.Translations.Select(t => new PoiTranslationDto
        {
            Id = t.Id,
            LanguageCode = t.LanguageCode,
            Title = t.Title,
            Description = t.Description
        }).ToList()
    })
    .ToListAsync();
```

### JSON Response
```json
{
  "id": 1,
  "name": "Nhà hàng Phở 88",
  "latitude": 10.798955,
  "longitude": 106.70090,
  "status": "Approved",
  "rejectionReason": null,
  "translations": [...]
}
```

---

## The SpatialHelper Class In Action

### CreatePoint Example
```csharp
// Input: latitude=10.798955, longitude=106.70090
Point point = SpatialHelper.CreatePoint(10.798955, 106.70090);

// Internally:
// 1. GeometryFactory creates Point with coordinate (106.70090, 10.798955)
// 2. SRID is set to 4326 (WGS84)
// 3. Returns: Point { X=106.70090, Y=10.798955, SRID=4326 }
```

### GetLatitude / GetLongitude Example
```csharp
Point point = SpatialHelper.CreatePoint(10.798955, 106.70090);

double lat = SpatialHelper.GetLatitude(point);    // Returns 10.798955
double lng = SpatialHelper.GetLongitude(point);   // Returns 106.70090

// Safely handles null:
double latNull = SpatialHelper.GetLatitude(null); // Returns 0.0
```

---

## Database View of Stored Data

### SQL Query to View Stored Points
```sql
SELECT 
    [Id],
    [Name],
    [Location].STAsText() AS [LocationWKT],
    [Location].STSrid AS [SRID],
    [Location].Lat AS [Latitude],
    [Location].Long AS [Longitude]
FROM [Pois]
WHERE [Status] = 'Approved'
```

### Output
```
Id | Name                  | LocationWKT                    | SRID | Latitude  | Longitude
---|----------------------|--------------------------------|------|-----------|----------
1  | Nhà hàng Phở 88      | POINT (106.70090 10.798955)   | 4326 | 10.798955 | 106.70090
2  | Nhà hàng Cơm Tấm     | POINT (106.70234 10.801234)   | 4326 | 10.801234 | 106.70234
```

---

## Distance Query Example

### Finding POIs within 5km circle
```csharp
var centerPoint = SpatialHelper.CreatePoint(10.800, 106.701);

var poIsWithin5km = await _context.Pois
    .Where(p => 
        p.Location.Distance(centerPoint) <= 5000)  // 5000 meters = 5km
    .ToAsync();
```

### What Happens
1. Database calculates distance from each POI to center point using STDistance()
2. SQL Server uses spatial index for fast calculation
3. Only POIs ≤ 5000 meters are returned
4. Result: Fast, efficient, database-optimized query

---

## Key Takeaways

1. **Always use SpatialHelper** for all Point creation/extraction
2. **Clients still use Latitude/Longitude** - convert in controller
3. **Database stores everything as Point** with SRID 4326
4. **Distance queries use .Distance()** method - evaluated at DB level
5. **Extract coordinates for API responses** using GetLatitude/GetLongitude
6. **Performance benefit** comes from database-level spatial filtering


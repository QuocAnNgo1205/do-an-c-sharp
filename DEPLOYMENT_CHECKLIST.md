# NetTopologySuite Refactoring - Quick Checklist

## ✅ Pre-Implementation Checklist

- [x] All code changes completed
- [x] NuGet packages added to .csproj
- [x] SpatialHelper utility class created
- [x] All entity models updated
- [x] All DTOs updated
- [x] All controller endpoints refactored
- [x] DbContext spatial configuration complete
- [x] Program.cs UseNetTopologySuite() added
- [x] Documentation complete

---

## 📋 Files to Deploy

### Code Files (Required)
```
backend/VinhKhanhFoodTour.API/
├── ✅ Program.cs
├── ✅ VinhKhanhFoodTour.API.csproj
├── ✅ Models/PoiStatus.cs
├── ✅ Data/AppDbContext.cs
├── ✅ Controller/PoiController.cs
├── ✅ DTOs/PoiDto.cs
├── ✅ DTOs/CreatePoiRequest.cs
└── ✅ SpatialHelper.cs
```

### Documentation Files (Reference Only)
```
root/
├── INDEX.md
├── COMPLETION_SUMMARY.md
├── README_NETTOPOLOGYSUITE_REFACTORING.md
├── NETTOPOLOGYSUITE_QUICK_REFERENCE.md
├── NETTOPOLOGYSUITE_EXAMPLES.md
├── BEST_PRACTICES.md
└── VISUAL_SUMMARY.md
```

---

## 🚀 Implementation Steps

### Phase 1: Environment Setup (10 minutes)

- [ ] Backup current database
- [ ] Create development branch: `git checkout -b feat/nettopologysuite`
- [ ] Run `dotnet restore` in API project directory
- [ ] Verify build succeeds: `dotnet build`

### Phase 2: Database Migration (20 minutes)

```powershell
# Step 1: Create migration
cd backend/VinhKhanhFoodTour.API
dotnet ef migrations add AddSpatialLocationToPoiReplaceLatLng

# Step 2: Review generated migration file
# ⚠️ CHECK: Does it drop Latitude/Longitude columns?
# ⚠️ CHECK: Does it add Location column with geography type?
# ⚠️ CHECK: Does it migrate existing data?

# Step 3: Apply to development database
dotnet ef database update

# Step 4: Verify in SQL Server Management Studio
SELECT Id, Location FROM Pois LIMIT 5;
```

- [ ] Migration file generated
- [ ] Migration reviewed for data safety
- [ ] Migration applied to dev database
- [ ] Verified in SQL Server (SELECT from Pois shows Location column)

### Phase 3: Testing (30 minutes)

```powershell
# Step 1: Start API
dotnet run

# Step 2: Test CreateNewPoi endpoint
curl -X POST http://localhost:5000/api/v1/poi \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{
    "name": "Test POI",
    "latitude": 10.798955,
    "longitude": 106.70090,
    "title": "Test",
    "description": "Test POI"
  }'

# Expected: 200 OK with { "message": "...", "id": N }

# Step 3: Test GetNearbyPois endpoint
curl "http://localhost:5000/api/v1/poi/public/nearby?userLat=10.8&userLng=106.7&radiusInMeters=1000"

# Expected: 200 OK with array of POIs

# Step 4: Run all tests
dotnet test
```

- [ ] API starts without errors
- [ ] POST /api/v1/poi creates POI successfully
- [ ] GET /api/v1/poi/public/nearby returns nearby POIs
- [ ] GET /api/v1/poi/public returns all approved POIs
- [ ] GET /api/v1/poi/pending returns pending POIs
- [ ] Unit tests pass

### Phase 4: Staging Validation (20 minutes)

- [ ] Deploy to staging environment
- [ ] Verify database migration on staging
- [ ] Run integration tests on staging
- [ ] Performance test /nearby endpoint
- [ ] Verify all API responses have Latitude/Longitude
- [ ] Confirm no breaking changes for mobile app

### Phase 5: Production Deployment (30 minutes)

- [ ] Finalize change set
- [ ] Request deployment approval
- [ ] Backup production database
- [ ] Deploy code changes
- [ ] Run database migration
- [ ] Verify endpoints working in production
- [ ] Create spatial index (optional but recommended)
- [ ] Monitor performance metrics

---

## 🧪 Testing Scenarios

### Test 1: Create POI
```
POST /api/v1/poi
{
  "name": "Phở 88",
  "latitude": 10.798955,
  "longitude": 106.70090,
  "title": "Phở Sài Gòn",
  "description": "Best pho in town"
}

Expected:
HTTP 200 OK
{ "message": "...", "id": 1 }
```
- [ ] Passes

### Test 2: Get Nearby POIs
```
GET /api/v1/poi/public/nearby?userLat=10.798&userLng=106.701&radiusInMeters=1000

Expected:
HTTP 200 OK
[
  {
    "id": 1,
    "name": "Phở 88",
    "latitude": 10.798955,
    "longitude": 106.70090,
    "status": "Approved",
    ...
  }
]
```
- [ ] Passes

### Test 3: Update POI
```
PUT /api/v1/poi/owner/1
{
  "name": "Phở 88 - Extended",
  "latitude": 10.799,
  "longitude": 106.702
}

Expected:
HTTP 200 OK
{ "message": "...", "id": 1 }
```
- [ ] Passes

### Test 4: List My POIs
```
GET /api/v1/poi/owner/my-pois
Authorization: Bearer {token}

Expected:
HTTP 200 OK
[
  {
    "id": 1,
    "name": "...",
    "latitude": ...,
    "longitude": ...,
    ...
  }
]
```
- [ ] Passes

### Test 5: Boundary Cases
- [ ] Latitude at boundary: -90.0 works
- [ ] Latitude at boundary: 90.0 works
- [ ] Longitude at boundary: -180.0 works
- [ ] Longitude at boundary: 180.0 works
- [ ] Invalid latitude (-91): Rejected
- [ ] Invalid longitude (181): Rejected
- [ ] Zero radius: Works correctly
- [ ] Large radius (10000km): Works correctly
- [ ] No results in radius: Returns empty array

---

## 📊 Performance Validation

### Before Refactoring (Baseline)
Run `/nearby` endpoint with 10,000 POIs in database:

```
curl "http://dev/api/v1/poi/public/nearby?userLat=10.8&userLng=106.7&radiusInMeters=1000"
```

Record:
- Response time: ______ ms
- Data transferred: ______ KB
- Results returned: ______ POIs

### After Refactoring
Run same endpoint:

```
curl "http://dev/api/v1/poi/public/nearby?userLat=10.8&userLng=106.7&radiusInMeters=1000"
```

Record:
- Response time: ______ ms (should be 20-100x faster)
- Data transferred: ______ KB (should be much smaller)
- Results returned: ______ POIs (should be same)

Expected improvement: **20-100x faster** on `/nearby` endpoint

---

## 🔍 SQL Server Verification

### Check Location Column Type
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Pois' AND COLUMN_NAME = 'Location'
```

Expected output:
```
COLUMN_NAME | DATA_TYPE | IS_NULLABLE
Location    | geography | NO
```

- [ ] Location column exists
- [ ] Type is "geography" (not "geometry")
- [ ] IS_NULLABLE is NO

### Verify Data in Location Column
```sql
SELECT TOP 5 Id, Name, Location.STAsText() as LocationWKT
FROM Pois
WHERE Status = 'Approved'
```

Expected output:
```
Id | Name      | LocationWKT
1  | Phở 88    | POINT (106.70090 10.798955)
2  | Cơm Tấm   | POINT (106.70234 10.801234)
...
```

- [ ] Location column populated
- [ ] Points are in format: POINT (longitude latitude)
- [ ] All POIs have Location set

### Test Spatial Distance Query (Optional)
```sql
DECLARE @userLocation GEOGRAPHY = GEOGRAPHY::Point(106.701, 10.800, 4326);

SELECT Id, Name, Location.STDistance(@userLocation) as DistanceMeters
FROM Pois
WHERE Status = 'Approved'
  AND Location.STDistance(@userLocation) <= 1000
ORDER BY Location.STDistance(@userLocation)
```

Expected output shows distances ≤ 1000 meters

- [ ] Query executes successfully
- [ ] Distances calculated correctly
- [ ] Only POIs within 1000m returned

---

## 📈 Monitoring & Alerts (Post-Deployment)

### Metrics to Monitor

- [ ] API response time for /nearby endpoint (target: <100ms)
- [ ] Error rate for spatial operations (target: 0%)
- [ ] Database query CPU usage (should decrease)
- [ ] Network transfer size (should decrease significantly)
- [ ] Number of POIs returned on /nearby (should be consistent)

### Queries to Run

```sql
-- Check spatial index usage
SELECT * FROM sys.spatial_indexes
WHERE object_id = OBJECT_ID('dbo.Pois')

-- Check query performance
SET STATISTICS IO ON;
SELECT * FROM Pois 
WHERE Status = 'Approved' 
  AND Location.STDistance(GEOGRAPHY::Point(...)) <= 1000;
SET STATISTICS IO OFF;
```

- [ ] Spatial index created successfully
- [ ] Query plans use spatial index
- [ ] No timeouts or errors

---

## 🐛 Rollback Plan

If issues occur:

```powershell
# Step 1: Identify issue
dotnet run
# ... test endpoints ...

# Step 2: Rollback migration
dotnet ef database update {previousMigrationName}

# Step 3: Revert code
git checkout main -- backend/VinhKhanhFoodTour.API/

# Step 4: Rebuild and test
dotnet build
dotnet test

# Step 5: Redeploy previous version
```

- [ ] Rollback procedure documented
- [ ] Previous migration name recorded
- [ ] Git branch backed up

---

## ✨ Post-Deployment

### Create Spatial Index (For Production)
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

- [ ] Spatial index created on production
- [ ] Performance verified
- [ ] Monitoring in place

### Documentation Updates

- [ ] Update API documentation (Swagger)
- [ ] Update team wiki
- [ ] Update deployment runbooks
- [ ] Notify team of changes

### Team Communication

- [ ] Notify frontend team (no changes needed)
- [ ] Notify QA team (spatial data now in DB)
- [ ] Notify database team (new index)
- [ ] Add to release notes

---

## 📞 Support

### If Something Goes Wrong

1. **API won't start**: Check NuGet packages installed (`dotnet restore`)
2. **Migration fails**: Review migration script, check for syntax errors
3. **/nearby returns wrong results**: Verify SRID 4326 is correct
4. **Slow queries**: Create spatial index (if not already created)
5. **SRID mismatch errors**: Check all Points use SpatialHelper

### Quick Troubleshooting Commands

```powershell
# Rebuild solution
dotnet clean
dotnet build

# Restore NuGet packages
dotnet restore

# Check database connection
dotnet ef database update --dry-run

# Run specific test
dotnet test --filter "TestName"

# View migration history
dotnet ef migrations list
```

---

## ✅ Sign-Off Checklist

- [ ] All code changes deployed
- [ ] Database migration applied
- [ ] All API endpoints tested
- [ ] Performance verified
- [ ] No errors in logs
- [ ] Team notified
- [ ] Documentation updated
- [ ] Spatial index created (prod)
- [ ] Monitoring alerts in place
- [ ] Rollback plan documented

---

## 📅 Timeline Estimate

| Phase | Duration | Status |
|-------|----------|--------|
| Setup | 10 min | ⏳ |
| Migration | 20 min | ⏳ |
| Testing | 30 min | ⏳ |
| Staging | 20 min | ⏳ |
| Production | 30 min | ⏳ |
| **Total** | **~110 min** | ⏳ |

---

**Last Updated**: April 3, 2026
**Version**: 1.0
**Status**: Ready to Deploy


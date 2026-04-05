using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.DTOs;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Services
{
    public class PoiService : IPoiService
    {
        private readonly AppDbContext _context;
        private readonly ISyncOrchestrator _syncOrchestrator;

        public PoiService(AppDbContext context, ISyncOrchestrator syncOrchestrator)
        {
            _context = context;
            _syncOrchestrator = syncOrchestrator;
        }

        public async Task<Poi> CreatePoiAsync(int ownerId, CreatePoiRequest request)
        {
            var location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);
            var newPoi = new Poi
            {
                OwnerId = ownerId,
                Name = request.Name,
                Location = location,

                // 👉 ĐÃ THÊM 2 DÒNG NÀY ĐỂ LƯU TỌA ĐỘ VÀO DATABASE
                Latitude = request.Latitude,
                Longitude = request.Longitude,

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

            _context.Pois.Add(newPoi);
            await _context.SaveChangesAsync();
            return newPoi;
        }

        public async Task<List<PoiDto>> GetPendingPoisAsync()
        {
            return await _context.Pois
                .Where(p => p.Status == PoiStatus.Pending)
                .Select(ToPoiDto())
                .ToListAsync();
        }

        public async Task<Poi> ApprovePoiAsync(int id)
        {
            var poi = await _context.Pois.FindAsync(id) ?? throw new KeyNotFoundException($"Không tìm thấy quán với ID: {id}");
            poi.Status = PoiStatus.Approved;
            poi.LastUpdated = DateTime.UtcNow;
            _context.Pois.Update(poi);
            await _context.SaveChangesAsync();
            await _syncOrchestrator.TryRefreshOfflinePackAsync();
            return poi;
        }

        public async Task<Poi> RejectPoiAsync(int id, string reason)
        {
            var poi = await _context.Pois.FindAsync(id) ?? throw new KeyNotFoundException($"Không tìm thấy quán với ID: {id}");
            poi.Status = PoiStatus.Rejected;
            poi.RejectionReason = reason;
            poi.LastUpdated = DateTime.UtcNow;
            _context.Pois.Update(poi);
            await _context.SaveChangesAsync();
            await _syncOrchestrator.TryRefreshOfflinePackAsync();
            return poi;
        }

        public async Task<Poi> CreateOwnerPoiAsync(int ownerId, CreatePoiDto request)
        {
            var location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);
            var newPoi = new Poi
            {
                OwnerId = ownerId,
                Name = request.Name,
                Location = location,

                // 👉 ĐÃ THÊM 2 DÒNG NÀY ĐỂ LƯU TỌA ĐỘ VÀO DATABASE
                Latitude = request.Latitude,
                Longitude = request.Longitude,

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

            _context.Pois.Add(newPoi);
            await _context.SaveChangesAsync();
            return newPoi;
        }

        public async Task<Poi> UpdatePoiAsync(int id, int ownerId, UpdatePoiDto request)
        {
            var poi = await _context.Pois.FindAsync(id) ?? throw new KeyNotFoundException($"Không tìm thấy quán với ID: {id}");
            if (poi.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("Forbidden");
            }

            poi.Name = request.Name;
            poi.Location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);

            // 👉 ĐÃ THÊM 2 DÒNG NÀY ĐỂ CẬP NHẬT LẠI TỌA ĐỘ TRONG DATABASE
            poi.Latitude = request.Latitude;
            poi.Longitude = request.Longitude;

            poi.Status = PoiStatus.Pending;
            poi.RejectionReason = null;
            poi.LastUpdated = DateTime.UtcNow;
            _context.Pois.Update(poi);
            await _context.SaveChangesAsync();
            await _syncOrchestrator.TryRefreshOfflinePackAsync();
            return poi;
        }

        public async Task<List<PoiDto>> GetOwnerPoisAsync(int ownerId)
        {
            return await _context.Pois
                .Where(p => p.OwnerId == ownerId)
                .Select(ToPoiDto())
                .ToListAsync();
        }

        public async Task<List<PoiDto>> GetPublicPoisAsync()
        {
            return await _context.Pois
                .Where(p => p.Status == PoiStatus.Approved)
                .Select(ToPoiDto())
                .ToListAsync();
        }

        public async Task<List<MapPinDto>> GetMapPinsAsync()
        {
            return await _context.Pois
                .Where(p => p.Status == PoiStatus.Approved)
                .Select(p => new MapPinDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Latitude = SpatialHelper.GetLatitude(p.Location),
                    Longitude = SpatialHelper.GetLongitude(p.Location)
                })
                .ToListAsync();
        }

        public async Task<List<PoiDto>> GetNearbyPoisAsync(double userLat, double userLng, double radiusInMeters)
        {
            var userLocation = SpatialHelper.CreatePoint(userLat, userLng);
            return await _context.Pois
                .Where(p => p.Status == PoiStatus.Approved && p.Location.Distance(userLocation) <= radiusInMeters)
                .Select(ToPoiDto())
                .ToListAsync();
        }

        public async Task<(Poi poi, PoiTranslation translation)> GetOwnerTranslationAsync(int poiId, string languageCode, int ownerId)
        {
            var poi = await _context.Pois.FindAsync(poiId) ?? throw new KeyNotFoundException($"Không tìm thấy quán với ID: {poiId}");
            if (poi.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("Forbidden");
            }

            var translation = await _context.PoiTranslations
                .FirstOrDefaultAsync(t => t.PoiId == poiId && t.LanguageCode == languageCode)
                ?? throw new KeyNotFoundException($"Không tìm thấy bản dịch cho ngôn ngữ: {languageCode}");

            return (poi, translation);
        }

        public async Task SaveTranslationAsync(PoiTranslation translation)
        {
            _context.PoiTranslations.Update(translation);
            await _context.SaveChangesAsync();
        }

        private static System.Linq.Expressions.Expression<Func<Poi, PoiDto>> ToPoiDto()
        {
            return p => new PoiDto
            {
                Id = p.Id,
                Name = p.Name,
                Latitude = SpatialHelper.GetLatitude(p.Location),
                Longitude = SpatialHelper.GetLongitude(p.Location),
                ImageUrl = p.ImageUrl ?? "",
                Status = p.Status,
                RejectionReason = p.RejectionReason,
                Translations = p.Translations.Select(t => new PoiTranslationDto
                {
                    Id = t.Id,
                    LanguageCode = t.LanguageCode,
                    Title = t.Title,
                    Description = t.Description
                }).ToList()
            };
        }
    }
}
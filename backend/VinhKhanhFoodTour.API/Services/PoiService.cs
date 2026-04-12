using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.DTOs;
using VinhKhanhFoodTour.Models;
using Microsoft.AspNetCore.Http;

namespace VinhKhanhFoodTour.API.Services
{
    public class PoiService : IPoiService
    {
        private readonly AppDbContext _context;
        private readonly ISyncOrchestrator _syncOrchestrator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PoiService(AppDbContext context, ISyncOrchestrator syncOrchestrator, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _syncOrchestrator = syncOrchestrator;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null) return string.Empty;
            return $"{request.Scheme}://{request.Host}{request.PathBase}";
        }

        /// <summary>
        /// Xóa file an toàn: chỉ xóa nếu đường dẫn thực sự nằm TRONG wwwroot.
        /// Chống lỗ hổng Path Traversal (ví dụ: ../../appsettings.json).
        /// </summary>
        private void SafeDeleteFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            var wwwroot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));
            var fullPath = Path.GetFullPath(Path.Combine(wwwroot, relativePath.TrimStart('/', '\\')));

            // Bảo vệ: đường dẫn phải nằm TRONG wwwroot
            if (!fullPath.StartsWith(wwwroot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[SECURITY] Path traversal attempt blocked: '{relativePath}' resolved to '{fullPath}'");
                return;
            }

            if (File.Exists(fullPath))
                File.Delete(fullPath);
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
            var baseUrl = GetBaseUrl();
            return await _context.Pois
                .Include(p => p.Translations)
                .Where(p => p.Status == PoiStatus.Pending)
                .Select(ToPoiDto(baseUrl))
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
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                // � FIX #1: ImageUrl chỉ nên được lưu vào Poi chính, KHÔNG phải PoiTranslation
                ImageUrl = request.ImageUrl ?? string.Empty,
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
                        // ❌ REMOVED: ImageUrl không nên được set ở đây (dành cho translation-specific images)
                    }
                }
            };

            Console.WriteLine($"[CreateOwnerPoiAsync] Creating Poi with ImageUrl={newPoi.ImageUrl}");

            _context.Pois.Add(newPoi);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[CreateOwnerPoiAsync] Poi saved to DB with Id={newPoi.Id}, ImageUrl={newPoi.ImageUrl}");

            return newPoi;
        }

        public async Task<Poi> UpdatePoiAsync(int id, int ownerId, UpdatePoiFormRequest request, string? imageUrl)
        {
            var poi = await _context.Pois
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == id) 
                ?? throw new KeyNotFoundException($"Không tìm thấy quán với ID: {id}");
                
            if (poi.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("Forbidden");
            }

            poi.Name = request.Name;
            poi.Latitude = request.Latitude;
            poi.Longitude = request.Longitude;
            poi.Location = SpatialHelper.CreatePoint(request.Latitude, request.Longitude);

            if (!string.IsNullOrEmpty(imageUrl))
            {
                // Xóa ảnh cũ nếu có (an toàn, chống Path Traversal)
                SafeDeleteFile(poi.ImageUrl);
                poi.ImageUrl = imageUrl;
            }

            // Cập nhật bản dịch tiếng Việt mặc định
            var viTranslation = poi.Translations.FirstOrDefault(t => t.LanguageCode == "vi");
            if (viTranslation != null)
            {
                viTranslation.Title = request.Title;
                viTranslation.Description = request.Description;
            }

            poi.Status = PoiStatus.Pending;
            poi.RejectionReason = null;
            poi.LastUpdated = DateTime.UtcNow;
            
            _context.Pois.Update(poi);
            await _context.SaveChangesAsync();
            await _syncOrchestrator.TryRefreshOfflinePackAsync();
            return poi;
        }

        public async Task DeletePoiAsync(int id, int userId, bool isAdmin)
        {
            var poi = await _context.Pois
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Không tìm thấy quán với ID: {id}");

            if (!isAdmin && poi.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa quán này.");
            }

            // Xóa file vật lý (an toàn, chống Path Traversal)
            SafeDeleteFile(poi.ImageUrl);

            foreach (var trans in poi.Translations)
            {
                SafeDeleteFile(trans.AudioFilePath);
                SafeDeleteFile(trans.ImageUrl);
            }

            _context.Pois.Remove(poi);
            await _context.SaveChangesAsync();
            await _syncOrchestrator.TryRefreshOfflinePackAsync();
        }

        public async Task<Poi> GetPoiByIdAsync(int id, int userId, bool isAdmin)
        {
            var poi = await _context.Pois
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException($"Không tìm thấy quán với ID: {id}");

            if (!isAdmin && poi.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("Forbidden");
            }

            return poi;
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
            var baseUrl = GetBaseUrl();
            return await _context.Pois
                .Include(p => p.Translations)
                .Where(p => p.OwnerId == ownerId)
                .Select(ToPoiDto(baseUrl))
                .ToListAsync();
        }

        public async Task<List<PoiDto>> GetPublicPoisAsync()
        {
            var baseUrl = GetBaseUrl();
            return await _context.Pois
                .Include(p => p.Translations)
                .Where(p => p.Status == PoiStatus.Approved)
                .Select(ToPoiDto(baseUrl))
                .ToListAsync();
        }

        public async Task<PoiDetailDto> GetPublicPoiByIdAsync(int id)
        {
            var poi = await _context.Pois
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == id && p.Status == PoiStatus.Approved);

            if (poi == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy quán truy cập công khai với ID: {id}");
            }

            var baseUrl = GetBaseUrl();
            return new PoiDetailDto
            {
                Id = poi.Id,
                Name = poi.Name,
                Latitude = SpatialHelper.GetLatitude(poi.Location),
                Longitude = SpatialHelper.GetLongitude(poi.Location),
                Translations = poi.Translations.Select(t => new PoiDetailTranslationDto
                {
                    Id = t.Id,
                    LanguageCode = t.LanguageCode,
                    Title = t.Title,
                    Description = t.Description ?? string.Empty,
                    AudioFilePath = string.IsNullOrEmpty(t.AudioFilePath) ? "" : (t.AudioFilePath.StartsWith("http") ? t.AudioFilePath : baseUrl.TrimEnd('/') + "/" + t.AudioFilePath.TrimStart('/')),
                    ImageUrl = string.IsNullOrEmpty(t.ImageUrl) ? "" : (t.ImageUrl.StartsWith("http") ? t.ImageUrl : baseUrl.TrimEnd('/') + "/" + t.ImageUrl.TrimStart('/'))
                }).ToList()
            };
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
            var baseUrl = GetBaseUrl();
            var userLocation = SpatialHelper.CreatePoint(userLat, userLng);
            return await _context.Pois
                .Where(p => p.Status == PoiStatus.Approved && p.Location.Distance(userLocation) <= radiusInMeters)
                .Select(ToPoiDto(baseUrl))
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

        private static System.Linq.Expressions.Expression<Func<Poi, PoiDto>> ToPoiDto(string baseUrl)
        {
            return p => new PoiDto
            {
                Id = p.Id,
                Name = p.Name,
                Latitude = SpatialHelper.GetLatitude(p.Location),
                Longitude = SpatialHelper.GetLongitude(p.Location),
                // 🌐 FIX: Đảm bảo không bị lặp dấu gạch chéo
                ImageUrl = string.IsNullOrEmpty(p.ImageUrl) ? "" : (p.ImageUrl.StartsWith("http") ? p.ImageUrl : baseUrl.TrimEnd('/') + "/" + p.ImageUrl.TrimStart('/')),
                Status = p.Status,
                RejectionReason = p.RejectionReason,
                Translations = p.Translations.Select(t => new PoiTranslationDto
                {
                    Id = t.Id,
                    LanguageCode = t.LanguageCode,
                    Title = t.Title,
                    Description = t.Description ?? string.Empty
                }).ToList()
            };
        }
    }
}
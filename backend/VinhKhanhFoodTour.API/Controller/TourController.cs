using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.DTOs;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TourController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TourController(AppDbContext context)
        {
            _context = context;
        }

        // ──────────────────────────────────────────────────────────
        // GET /api/v1/Poi/builder-pool?sortBy=popularity|name
        // POI pool for the Tour Builder, sorted by engagement
        // ──────────────────────────────────────────────────────────
        [HttpGet("/api/v1/Poi/builder-pool")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetPoiPool([FromQuery] string sortBy = "popularity")
        {
            try
            {
                var pois = await _context.Pois
                    .Where(p => p.Status == PoiStatus.Approved)
                    .Select(p => new PoiSummaryDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ImageUrl = p.ImageUrl,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Status = p.Status.ToString(),
                        NarrationCount = _context.NarrationLogs.Count(n => n.PoiId == p.Id),
                        QrScanCount = _context.QrScanLogs.Count(q => q.PoiId == p.Id)
                    })
                    .ToListAsync();

                var sorted = sortBy.ToLower() switch
                {
                    "popularity" => pois.OrderByDescending(p => p.PopularityScore).ToList(),
                    _ => pois.OrderBy(p => p.Name).ToList()
                };

                return Ok(sorted);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách POI: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────
        // POST /api/v1/Tour/suggest-route
        // Nearest-neighbour route from the most popular POI
        // ──────────────────────────────────────────────────────────
        [HttpPost("suggest-route")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> SuggestRoute([FromBody] SuggestRouteRequest request)
        {
            if (request.PoiIds == null || request.PoiIds.Count < 2)
                return BadRequest(new { Message = "Cần ít nhất 2 POI để gợi ý tuyến đường." });

            try
            {
                // Fetch the requested POIs with their popularity stats
                var pois = await _context.Pois
                    .Where(p => request.PoiIds.Contains(p.Id))
                    .Select(p => new SuggestRouteItemDto
                    {
                        PoiId = p.Id,
                        PoiName = p.Name,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        PopularityScore =
                            _context.NarrationLogs.Count(n => n.PoiId == p.Id) +
                            _context.QrScanLogs.Count(q => q.PoiId == p.Id)
                    })
                    .ToListAsync();

                var ordered = NearestNeighbourSort(pois);
                return Ok(ordered);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tính tuyến đường: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────
        // GET /api/v1/Tour — List all tours with usage counts
        // ──────────────────────────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetTours()
        {
            try
            {
                var tours = await _context.Tours
                    .Include(t => t.TourPois)
                        .ThenInclude(tp => tp.Poi)
                    .Include(t => t.UsageLogs)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new TourResponseDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        EstimatedPrice = t.EstimatedPrice,
                        ThumbnailUrl = t.ThumbnailUrl,
                        CreatedAt = t.CreatedAt,
                        UsageCount = t.UsageLogs.Count,
                        Pois = t.TourPois.OrderBy(tp => tp.OrderIndex).Select(tp => new TourPoiResponseDto
                        {
                            PoiId = tp.PoiId,
                            OrderIndex = tp.OrderIndex,
                            PoiName = tp.Poi != null ? tp.Poi.Name : string.Empty,
                            PoiImageUrl = tp.Poi != null ? tp.Poi.ImageUrl : null,
                            Latitude = tp.Poi != null ? tp.Poi.Latitude : 0,
                            Longitude = tp.Poi != null ? tp.Poi.Longitude : 0
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(tours);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách tour: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────
        // GET /api/v1/Tour/{id}
        // ──────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetTour(int id)
        {
            try
            {
                var tour = await _context.Tours
                    .Include(t => t.TourPois)
                        .ThenInclude(tp => tp.Poi)
                    .Include(t => t.UsageLogs)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tour == null)
                    return NotFound(new { Message = "Không tìm thấy tour." });

                var dto = new TourResponseDto
                {
                    Id = tour.Id,
                    Title = tour.Title,
                    Description = tour.Description,
                    EstimatedPrice = tour.EstimatedPrice,
                    ThumbnailUrl = tour.ThumbnailUrl,
                    CreatedAt = tour.CreatedAt,
                    UsageCount = tour.UsageLogs.Count,
                    Pois = tour.TourPois.OrderBy(tp => tp.OrderIndex).Select(tp => new TourPoiResponseDto
                    {
                        PoiId = tp.PoiId,
                        OrderIndex = tp.OrderIndex,
                        PoiName = tp.Poi?.Name ?? string.Empty,
                        PoiImageUrl = tp.Poi?.ImageUrl,
                        Latitude = tp.Poi?.Latitude ?? 0,
                        Longitude = tp.Poi?.Longitude ?? 0
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy tour: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────
        // POST /api/v1/Tour/{id}/log-usage
        // Mobile/Web app calls this when a user starts a tour
        // ──────────────────────────────────────────────────────────
        [HttpPost("{id}/log-usage")]
        [AllowAnonymous]
        public async Task<IActionResult> LogUsage(int id, [FromBody] TourUsageLogRequest? request)
        {
            try
            {
                var exists = await _context.Tours.AnyAsync(t => t.Id == id);
                if (!exists)
                    return NotFound(new { Message = "Không tìm thấy tour." });

                var log = new TourUsageLog
                {
                    TourId = id,
                    DeviceId = request?.DeviceId,
                    Timestamp = DateTime.UtcNow
                };

                _context.TourUsageLogs.Add(log);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Đã ghi nhận lượt sử dụng tour.", Id = log.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi ghi nhận lượt sử dụng: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────
        // GET /api/v1/Tour/stats — Aggregated usage stats for all tours
        // ──────────────────────────────────────────────────────────
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> GetTourStats()
        {
            try
            {
                var stats = await _context.Tours
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        UsageCount = t.UsageLogs.Count,
                        StopCount = t.TourPois.Count,
                        t.EstimatedPrice
                    })
                    .OrderByDescending(t => t.UsageCount)
                    .ToListAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy thống kê: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────
        // POST /api/v1/Tour — Create a new tour
        // ──────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> CreateTour([FromBody] TourCreateDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { Message = "Tên tour không được để trống." });

            if (request.Pois == null || request.Pois.Count == 0)
                return BadRequest(new { Message = "Tour phải có ít nhất một điểm dừng." });

            try
            {
                var tour = new Tour
                {
                    Title = request.Title,
                    Description = request.Description,
                    EstimatedPrice = request.EstimatedPrice,
                    ThumbnailUrl = request.ThumbnailUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tours.Add(tour);
                await _context.SaveChangesAsync();

                var tourPois = request.Pois.Select((item, idx) => new TourPoi
                {
                    TourId = tour.Id,
                    PoiId = item.PoiId,
                    OrderIndex = item.OrderIndex > 0 ? item.OrderIndex : idx + 1
                }).ToList();

                _context.TourPois.AddRange(tourPois);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, new
                {
                    Message = "Tạo tour thành công!",
                    Id = tour.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tạo tour: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────
        // PUT /api/v1/Tour/{id} — Update tour and re-sync POI order
        // ──────────────────────────────────────────────────────────
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Owner")]
        public async Task<IActionResult> UpdateTour(int id, [FromBody] TourUpdateDto request)
        {
            try
            {
                var tour = await _context.Tours
                    .Include(t => t.TourPois)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tour == null)
                    return NotFound(new { Message = "Không tìm thấy tour." });

                if (!string.IsNullOrWhiteSpace(request.Title)) tour.Title = request.Title;
                if (request.Description != null) tour.Description = request.Description;
                if (request.EstimatedPrice.HasValue) tour.EstimatedPrice = request.EstimatedPrice.Value;
                if (request.ThumbnailUrl != null) tour.ThumbnailUrl = request.ThumbnailUrl;

                if (request.Pois != null && request.Pois.Count > 0)
                {
                    _context.TourPois.RemoveRange(tour.TourPois);
                    var newTourPois = request.Pois.Select((item, idx) => new TourPoi
                    {
                        TourId = tour.Id,
                        PoiId = item.PoiId,
                        OrderIndex = item.OrderIndex > 0 ? item.OrderIndex : idx + 1
                    }).ToList();
                    _context.TourPois.AddRange(newTourPois);
                }

                await _context.SaveChangesAsync();
                return Ok(new { Message = "Cập nhật tour thành công!", Id = tour.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi cập nhật tour: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────
        // DELETE /api/v1/Tour/{id}
        // ──────────────────────────────────────────────────────────
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTour(int id)
        {
            try
            {
                var tour = await _context.Tours.FindAsync(id);
                if (tour == null)
                    return NotFound(new { Message = "Không tìm thấy tour." });

                _context.Tours.Remove(tour);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Đã xóa tour thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi xóa tour: " + ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Nearest-neighbour greedy sort.
        /// 1. Start from the POI with the highest popularity score.
        /// 2. Always go to the geographically closest unvisited POI next.
        /// </summary>
        private static List<SuggestRouteItemDto> NearestNeighbourSort(List<SuggestRouteItemDto> pois)
        {
            if (pois.Count <= 1) return pois;

            var remaining = pois.ToList();
            var result = new List<SuggestRouteItemDto>();

            // Start from the most popular POI
            var current = remaining.OrderByDescending(p => p.PopularityScore).First();
            remaining.Remove(current);
            result.Add(current);

            while (remaining.Count > 0)
            {
                var next = remaining
                    .OrderBy(p => HaversineKm(current.Latitude, current.Longitude, p.Latitude, p.Longitude))
                    .First();
                remaining.Remove(next);
                result.Add(next);
                current = next;
            }

            // Assign OrderIndex
            for (int i = 0; i < result.Count; i++)
                result[i].OrderIndex = i + 1;

            return result;
        }

        /// <summary>Haversine distance between two GPS coordinates in kilometres.</summary>
        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;
    }
}

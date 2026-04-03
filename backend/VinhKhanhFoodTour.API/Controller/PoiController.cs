using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VinhKhanhFoodTour.DTOs;
using VinhKhanhFoodTour.API.Services;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Owner")]
    public class PoiController : ControllerBase
    {
        private readonly IPoiService _poiService;
        private readonly IMediaService _mediaService;
        private readonly ISyncOrchestrator _syncOrchestrator;

        public PoiController(IPoiService poiService, IMediaService mediaService, ISyncOrchestrator syncOrchestrator)
        {
            _poiService = poiService;
            _mediaService = mediaService;
            _syncOrchestrator = syncOrchestrator;
        }

        // API: Thêm một quán ăn mới
        [HttpPost]
        public async Task<IActionResult> CreateNewPoi([FromBody] CreatePoiRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var ownerId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng từ token." });
                }

                var newPoi = await _poiService.CreatePoiAsync(ownerId, request);

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

        // API: Lấy danh sách các quán đang chờ duyệt (Admin only)
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingPois()
        {
            try
            {
                var pendingPois = await _poiService.GetPendingPoisAsync();

                return Ok(pendingPois);
            }
            catch (Exception)
            {
                return StatusCode(500, "Đã xảy ra lỗi khi lấy danh sách quán ăn chờ duyệt.");
            }
        }

        // API: Duyệt một quán ăn (Admin only)
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePoi(int id)
        {
            try
            {
                var poi = await _poiService.ApprovePoiAsync(id);

                return Ok(new { Message = "Đã duyệt quán ăn thành công!", Id = poi.Id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi duyệt quán: " + ex.Message });
            }
        }

        // API: Từ chối một quán ăn (Admin only)
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectPoi(int id, [FromBody] RejectPoiDto request)
        {
            try
            {
                var poi = await _poiService.RejectPoiAsync(id, request.Reason);

                return Ok(new { Message = "Đã từ chối quán ăn thành công!", Id = poi.Id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi từ chối quán: " + ex.Message });
            }
        }

        // API: Chủ quán tạo một quán ăn mới (Owner/Admin)
        [HttpPost("owner/create")]
        [Authorize(Roles = "Owner,Admin")]
        public async Task<IActionResult> CreatePoi([FromBody] CreatePoiDto request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var ownerId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                var newPoi = await _poiService.CreateOwnerPoiAsync(ownerId, request);

                return CreatedAtAction(nameof(CreatePoi), new { id = newPoi.Id }, new { Message = "Đã tạo quán ăn thành công! Chờ Admin duyệt.", Id = newPoi.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tạo quán: " + ex.Message });
            }
        }

        // API: Chủ quán cập nhật quán ăn của mình (Owner only)
        [HttpPut("owner/{id}")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UpdatePoi(int id, [FromBody] UpdatePoiDto request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var currentUserId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                var poi = await _poiService.UpdatePoiAsync(id, currentUserId, request);

                return Ok(new { Message = "Đã cập nhật quán ăn thành công! Chờ Admin duyệt lại.", Id = poi.Id });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi cập nhật quán: " + ex.Message });
            }
        }

        // API: Lấy danh sách quán ăn của chủ quán hiện tại (Owner only)
        [HttpGet("owner/my-pois")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> GetMyPois()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var ownerIdForList))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng." });
                }

                var myPois = await _poiService.GetOwnerPoisAsync(ownerIdForList);

                return Ok(myPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách quán của bạn: " + ex.Message });
            }
        }

        // API: Lấy danh sách các quán ăn đã được duyệt (Public)
        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicPois()
        {
            try
            {
                var approvedPois = await _poiService.GetPublicPoisAsync();

                return Ok(approvedPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách quán công khai: " + ex.Message });
            }
        }

        // API: Lấy danh sách các chân dung quán trên bản đồ (Public - tối ưu cho bản đồ)
        [HttpGet("public/map-pins")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMapPins()
        {
            try
            {
                var mapPins = await _poiService.GetMapPinsAsync();

                return Ok(mapPins);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách chân dung quán: " + ex.Message });
            }
        }

        // API: Lấy danh sách các quán ăn gần người dùng (Public - dựa trên vị trí)
        // Spatial query performed at the database level using NetTopologySuite
        [HttpGet("public/nearby")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNearbyPois([FromQuery] double userLat, [FromQuery] double userLng, [FromQuery] double radiusInMeters = 50)
        {
            try
            {
                var nearbyPois = await _poiService.GetNearbyPoisAsync(userLat, userLng, radiusInMeters);

                return Ok(nearbyPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi lấy danh sách quán gần đây: " + ex.Message });
            }
        }

        // API: Chủ quán tải lên tệp media (ảnh/âm thanh) cho bản dịch của quán (Owner only)
        [HttpPost("owner/{id}/translations/{languageCode}/media")]
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> UploadMediaForTranslation(int id, string languageCode, IFormFile? imageFile, IFormFile? audioFile)
        {
            try
            {
                // 1. Extract current user's ID from JWT claims
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
                {
                    return Unauthorized(new { Message = "Không thể xác định người dùng từ token." });
                }

                if (imageFile == null && audioFile == null)
                {
                    return BadRequest(new { Message = "Vui lòng cung cấp ít nhất một tệp ảnh hoặc âm thanh." });
                }

                var (poi, translation) = await _poiService.GetOwnerTranslationAsync(id, languageCode, currentUserId);
                var saved = await _mediaService.SaveMediaAsync(imageFile, audioFile);
                if (saved.imageUrl != null)
                {
                    translation.ImageUrl = saved.imageUrl;
                }
                if (saved.audioFilePath != null)
                {
                    translation.AudioFilePath = saved.audioFilePath;
                }

                await _poiService.SaveTranslationAsync(translation);
                if (poi.Status == PoiStatus.Approved)
                {
                    await _syncOrchestrator.TryRefreshOfflinePackAsync();
                }

                // Return Ok with updated ImageUrl and AudioFilePath
                return Ok(new
                {
                    Message = "Đã tải lên tệp media thành công!",
                    ImageUrl = translation.ImageUrl,
                    AudioFilePath = translation.AudioFilePath
                });
            }
            catch (IOException ioEx)
            {
                return StatusCode(500, new { Message = "Lỗi tệp hệ thống: " + ioEx.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi tải lên tệp media: " + ex.Message });
            }
        }
    }
}
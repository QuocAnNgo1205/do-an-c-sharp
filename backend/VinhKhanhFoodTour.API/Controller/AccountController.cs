using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // PUT: api/v1/Account/language
        [HttpPut("language")]
        public async Task<IActionResult> UpdateLanguage([FromBody] LanguageUpdateRequest request)
        {
            // 1. Lấy UserId từ Token (Claim Sub)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                             ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { Message = "Không xác định được người dùng!" });
            }

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new { Message = "UserId không hợp lệ!" });
            }

            // 2. Tìm User trong DB
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "Người dùng không tồn tại!" });
            }

            // 3. Cập nhật mã ngôn ngữ
            user.PreferredLanguage = request.LanguageCode;
            
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Cập nhật ngôn ngữ thành công!", LanguageCode = user.PreferredLanguage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi khi lưu dữ liệu: {ex.Message}" });
            }
        }
    }

    public class LanguageUpdateRequest
    {
        public string LanguageCode { get; set; } = string.Empty;
    }
}

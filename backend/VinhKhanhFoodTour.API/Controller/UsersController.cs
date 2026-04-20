using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.Models;
using VinhKhanhFoodTour.API.DTOs;

namespace VinhKhanhFoodTour.API.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetUserStats()
        {
            var tourists = await _context.Users.Include(u => u.Role).CountAsync(u => u.Role != null && u.Role.RoleName == "Tourist");
            var owners = await _context.Users.Include(u => u.Role).CountAsync(u => u.Role != null && u.Role.RoleName == "Owner");
            
            // Define 'Active' as having any activity in the last 5 minutes
            var activeThreshold = DateTime.UtcNow.AddMinutes(-10);
            var activeTourists = await _context.UserDevices
                .Include(d => d.User)
                .ThenInclude(u => u!.Role)
                .Where(d => d.User != null && d.User.Role != null && d.User.Role.RoleName == "Tourist" && d.LastActiveAt >= activeThreshold)
                .Select(d => d.UserId)
                .Distinct()
                .CountAsync();

            return Ok(new { 
                TotalTourists = tourists, 
                TotalOwners = owners, 
                TotalUsers = tourists + owners,
                ActiveTourists = activeTourists
            });
        }

        // GET: api/v1/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Select(u => new
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    RoleName = u.Role != null ? u.Role.RoleName : "Unknown",
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        // PUT: api/v1/Users/{id}/toggle-status
        [HttpPut("{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Cannot block the main admin account (safety check)
            if (user.RoleId == 1 && user.IsActive) // Assuming RoleId 1 is Admin
            {
                // Verify if it's the only admin
                var adminCount = await _context.Users.CountAsync(u => u.RoleId == 1 && u.IsActive);
                if (adminCount <= 1)
                {
                    return BadRequest(new { Message = "Cannot block the last active admin account." });
                }
            }

            // Toggle active status
            user.IsActive = !user.IsActive;

            try
            {
                await _context.SaveChangesAsync();
                
                string action = user.IsActive ? "Unblocked" : "Blocked";
                return Ok(new { Message = $"User '{user.Username}' has been {action}." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the database." });
            }
        }
        // POST: api/v1/Users
        [HttpPost]
        public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            // 1. Kiểm tra tồn tại
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { Message = "Username đã tồn tại!" });
            }

            // 2. Tìm Role
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.RoleName);
            if (role == null)
            {
                return BadRequest(new { Message = $"Không tìm thấy Role '{request.RoleName}'" });
            }

            // 3. Tạo User, băm mật khẩu bằng BCrypt (giống luồng Register thông thường)
            var user = new VinhKhanhFoodTour.Models.User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // PHẢI hash!
                RoleId = role.Id,
                IsActive = true,
                PreferredLanguage = "vi"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Tạo User thành công!", Id = user.Id });
        }

        // DELETE: api/v1/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // 1. Tìm User
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { Message = "User không tồn tại!" });
            }

            // 2. 🛡️ Bảo vệ: Admin không được tự xóa chính mình
            var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (int.TryParse(currentUserIdClaim, out int currentAdminId) && currentAdminId == id)
            {
                return BadRequest(new { Message = "Bạn không thể tự xóa tài khoản Admin đang đăng nhập!" });
            }

            // 3. Xử lý xóa
            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok(new { Message = $"Đã xóa thành công User '{user.Username}'." });
            }
            catch (DbUpdateException ex)
            {
                // Thường do dính Foreign Key (ví dụ Owner đã tạo POI mà DB không để Cascade Delete)
                return BadRequest(new { 
                    Message = "Không thể xóa User này do ràng buộc dữ liệu (hệ thống có chứa các quán ăn/dữ liệu liên quan). Hãy chặn (Block) thay vì xóa.",
                    Detail = ex.InnerException?.Message 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống khi xóa User.", Detail = ex.Message });
            }
        }

        // GET: api/v1/Users/{id}/devices
        [HttpGet("{id}/devices")]
        public async Task<ActionResult<IEnumerable<UserDeviceDto>>> GetUserDevices(int id)
        {
            var devices = await _context.UserDevices
                .Where(d => d.UserId == id)
                .OrderByDescending(d => d.LastActiveAt)
                .Select(d => new UserDeviceDto
                {
                    Id = d.Id,
                    DeviceId = d.DeviceId,
                    DeviceName = d.DeviceName,
                    Os = d.Os,
                    LastActiveAt = d.LastActiveAt,
                    IsRevoked = d.IsRevoked
                })
                .ToListAsync();

            return Ok(devices);
        }

        // DELETE: api/v1/Users/{id}/devices/{deviceId}
        [HttpDelete("{id}/devices/{deviceId}")]
        public async Task<IActionResult> RevokeDevice(int id, string deviceId)
        {
            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == id && d.DeviceId == deviceId);

            if (device == null)
            {
                return NotFound(new { Message = "Không tìm thấy thiết bị!" });
            }

            _context.UserDevices.Remove(device);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đã thu hồi quyền truy cập của thiết bị thành công." });
        }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string RoleName { get; set; } = "Owner";
    }
}

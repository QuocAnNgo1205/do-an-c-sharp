using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VinhKhanhFoodTour.Data;

namespace VinhKhanhFoodTour.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 1. Tìm user trong DB theo Username
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu!" });
            }

            // Kiểm tra mật khẩu bằng BCrypt, HOẶC hỗ trợ fallback nếu mk cũ là PlainText
            bool isPasswordCorrect = false;
            try
            {
                isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch 
            {
                // Nếu PasswordHash không phải là mã BCrypt hợp lệ (VD: user tự seed tay là "0", "1")
                isPasswordCorrect = (user.PasswordHash == request.Password);
            }

            if (!isPasswordCorrect && user.PasswordHash != request.Password)
            {
                // Trường hợp BCrypt ném Exception ở trên nhưng phép gán cuối vẫn Fail
                // Hoặc BCrypt không throw Exception nhưng Verify() trả về false, ta cho phép kiểm tra fallback thêm lần nữa cho an toàn.
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu!" });
            }

            // 2. Tạo các thông tin (Claims) giấu vào trong Token
            var claims = new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Tourist") // Đính kèm chức vụ (Admin/Owner)
            };

            // 3. Ký tên và tạo mã Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"]!,
                audience: _configuration["Jwt:Audience"]!,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), // Token sống được 7 ngày
                signingCredentials: creds
            );

            // 4. Trả vé về cho Frontend
            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Role = user.Role.RoleName,
                Username = user.Username,
                Email = user.Email ?? "Chưa cập nhật email",
                Expiration = token.ValidTo
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 1. Kiểm tra username hoặc email đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { Message = "Username đã tồn tại!" });
            }

            if (!string.IsNullOrEmpty(request.Email) && await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { Message = "Email đã tồn tại!" });
            }

            // 2. Xác định Role (Mặc định là Tourist cho Mobile App)
            string roleToAssign = string.IsNullOrEmpty(request.Role) ? "Tourist" : request.Role;
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleToAssign);
            
            if (role == null)
            {
                return BadRequest(new { Message = $"Lỗi hệ thống: Không tìm thấy Role {roleToAssign}!" });
            }

            // 3. Tạo mã băm cho Mật Khẩu (Bảo mật)
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 4. Tạo User mới
            var user = new VinhKhanhFoodTour.Models.User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                RoleId = role.Id,
                IsActive = true,
                PreferredLanguage = "vi" // Mặc định tiếng Việt
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Đăng ký thành công!" });
        }
    }

    // Class phụ để nhận data từ Frontend gửi lên
    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Role { get; set; } // "Owner", "Tourist", v.v.
    }
}
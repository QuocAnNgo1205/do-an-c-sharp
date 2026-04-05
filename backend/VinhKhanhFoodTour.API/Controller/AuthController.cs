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
            // 1. Tìm user trong DB (Lúc seed data mình để pass là chữ "0", "1", "2")
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.PasswordHash == request.Password);

            if (user == null)
            {
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu!" });
            }

            // 2. Tạo các thông tin (Claims) giấu vào trong Token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.RoleName) // Đính kèm chức vụ (Admin/Owner)
            };

            // 3. Ký tên và tạo mã Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), // Token sống được 7 ngày
                signingCredentials: creds
            );

            // 4. Trả vé về cho Frontend
            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Role = user.Role.RoleName,
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

            // 2. Tìm Role "Owner" trong DB
            var ownerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Owner");
            if (ownerRole == null)
            {
                return StatusCode(500, new { Message = "Lỗi hệ thống: Không tìm thấy Role Owner!" });
            }

            // 3. Tạo User mới
            var user = new VinhKhanhFoodTour.Models.User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = request.Password, // Lưu trơn theo cơ chế hiện tại
                RoleId = ownerRole.Id,
                IsActive = true
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
    }
}
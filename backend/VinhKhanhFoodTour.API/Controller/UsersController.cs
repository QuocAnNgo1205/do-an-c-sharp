using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFoodTour.Data;
using VinhKhanhFoodTour.Models;

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
    }
}

using MedBridge.Models;
using MedBridge.Models.Messages;
using MedBridge.Models.ProductModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System.Security.Cryptography;
using System.Text;
using static MedBridge.Models.User;

namespace MedBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.users
               
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.MedicalSpecialist,
                    u.Address,
                    u.ProfileImage,
                    u.CreatedAt,
                    u.KindOfWork,
                    u.IsAdmin,
                    u.Status,
                    Products = u.Products.Select(p => new
                    {
                        p.ProductId,
                        p.Name,
                        p.Description
                    }).ToList(),
                    ContactUsMessages = u.ContactUs.Select(c => new
                    {
                        c.Id,
                        c.Message,
                        c.CreatedAt
                    }).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("add-admin")]
        public async Task<IActionResult> AddAdmin(int id)
        {
            var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
                return NotFound("User not found.");

            if (existingUser.IsAdmin)
                return BadRequest("Admin already exists.");

            existingUser.IsAdmin = true;
            await _context.SaveChangesAsync();

            return Ok("User promoted to admin.");
        }

        [HttpPut("delete-admin/{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try
            {
                var admin = await _context.users.FindAsync(id);
                if (admin == null || !admin.IsAdmin)
                    return NotFound("Admin not found.");

                if (admin.Id == 1)
                    return BadRequest("Main admin cannot be deleted.");

                admin.IsAdmin = false;
                await _context.SaveChangesAsync();

                return Ok("Admin deleted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, "An error occurred: " + ex.Message);
            }
        }


        [HttpPost("block-user/{id}")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null || user.IsAdmin)
                return BadRequest("Invalid user.");

            user.Status = UserStatus.Blocked;
            await _context.SaveChangesAsync();
            return Ok("User blocked.");
        }


        [HttpPost("Un_block-user/{id}")]
        public async Task<IActionResult> UnBlockUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null || user.IsAdmin)
                return BadRequest("Invalid user.");

            user.Status = UserStatus.Active;
            await _context.SaveChangesAsync();
            return Ok("User Un blocked.");
        }

        [HttpPost("Activate-user/{id}")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null || user.IsAdmin)
                return BadRequest("Invalid user.");

            user.Status = UserStatus.Active;
            await _context.SaveChangesAsync();
            return Ok("User deactivated.");
        }
        [HttpPost("deactivate-user/{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null || user.IsAdmin)
                return BadRequest("Invalid user.");

            user.Status = UserStatus.Deactivated;
            await _context.SaveChangesAsync();
            return Ok("User deactivated.");
        }
    }
}

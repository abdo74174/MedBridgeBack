using MedBridge.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;

namespace MedBridge.Controllers.Info
{
    public class Specialties : Controller
    {

        private readonly ApplicationDbContext _context;

        public Specialties(ApplicationDbContext context)
        {
            _context = context;
        }

        private readonly ILogger<UserController> _logger;

        private async Task<bool> IsUserAdmin(string email)
        {
            var user = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null && user.IsAdmin;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("specialties")]
        public async Task<IActionResult> GetSpecialties()
        {
            try
            {
                var specialties = await _context.MedicalSpecialties.Select(ms => ms.Name).ToListAsync();
                return Ok(new { specialties });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSpecialties");
                return StatusCode(500, new { message = "An error occurred while retrieving specialties." });
            }
        }

        [HttpPost("specialties")]
        public async Task<IActionResult> AddSpecialty([FromBody] MedicalSpecialtyDto dto, [FromQuery] string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return Unauthorized(new { message = "Only admins can add specialties." });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Specialty name is required." });
                }

                var existingSpecialty = await _context.MedicalSpecialties.FirstOrDefaultAsync(ms => ms.Name == dto.Name);
                if (existingSpecialty != null)
                {
                    return BadRequest(new { message = "Specialty already exists." });
                }

                var specialty = new MedicalSpecialty { Name = dto.Name };
                _context.MedicalSpecialties.Add(specialty);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Specialty added successfully.", specialty = specialty.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddSpecialty");
                return StatusCode(500, new { message = "An error occurred while adding specialty." });
            }
        }

        [HttpPut("specialties/{name}")]
        public async Task<IActionResult> UpdateSpecialty(string name, [FromBody] MedicalSpecialtyDto dto, [FromQuery] string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return Unauthorized(new { message = "Only admins can update specialties." });
                }

                var specialty = await _context.MedicalSpecialties.FirstOrDefaultAsync(ms => ms.Name == name);
                if (specialty == null)
                {
                    return NotFound(new { message = "Specialty not found." });
                }

                specialty.Name = dto.Name;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Specialty updated successfully.", specialty = specialty.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSpecialty");
                return StatusCode(500, new { message = "An error occurred while updating specialty." });
            }
        }

        [HttpDelete("specialties/{name}")]
        public async Task<IActionResult> DeleteSpecialty(string name, [FromQuery] string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return Unauthorized(new { message = "Only admins can delete specialties." });
                }

                var specialty = await _context.MedicalSpecialties.FirstOrDefaultAsync(ms => ms.Name == name);
                if (specialty == null)
                {
                    return NotFound(new { message = "Specialty not found." });
                }

                _context.MedicalSpecialties.Remove(specialty);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Specialty deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSpecialty");
                return StatusCode(500, new { message = "An error occurred while deleting specialty." });
            }
        }

    }
}

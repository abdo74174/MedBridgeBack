using MedBridge.Models;
using MedBridge.Models.UserInfo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApi.models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public class SpecialtiesService : ISpecialtiesService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SpecialtiesService> _logger;

        public SpecialtiesService(ApplicationDbContext context, ILogger<SpecialtiesService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> IsUserAdmin(string email)
        {
            var user = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null && user.IsAdmin;
        }

        public async Task<IActionResult> GetSpecialties()
        {
            try
            {
                var specialties = await _context.MedicalSpecialties.Select(ms => ms.Name).ToListAsync();
                return new OkObjectResult(new { specialties });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSpecialties");
                return new ObjectResult(new { message = "An error occurred while retrieving specialties." })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> AddSpecialty(MedicalSpecialtyDto dto, string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return new UnauthorizedObjectResult(new { message = "Only admins can add specialties." });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return new BadRequestObjectResult(new { message = "Specialty name is required." });
                }

                var existingSpecialty = await _context.MedicalSpecialties.FirstOrDefaultAsync(ms => ms.Name == dto.Name);
                if (existingSpecialty != null)
                {
                    return new BadRequestObjectResult(new { message = "Specialty already exists." });
                }

                var specialty = new MedicalSpecialty { Name = dto.Name };
                _context.MedicalSpecialties.Add(specialty);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { message = "Specialty added successfully.", specialty = specialty.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddSpecialty");
                return new ObjectResult(new { message = "An error occurred while adding specialty." })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> UpdateSpecialty(string name, MedicalSpecialtyDto dto, string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return new UnauthorizedObjectResult(new { message = "Only admins can update specialties." });
                }

                var specialty = await _context.MedicalSpecialties.FirstOrDefaultAsync(ms => ms.Name == name);
                if (specialty == null)
                {
                    return new NotFoundObjectResult(new { message = "Specialty not found." });
                }

                specialty.Name = dto.Name;
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { message = "Specialty updated successfully.", specialty = specialty.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSpecialty");
                return new ObjectResult(new { message = "An error occurred while updating specialty." })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> DeleteSpecialty(string name, string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return new UnauthorizedObjectResult(new { message = "Only admins can delete specialties." });
                }

                var specialty = await _context.MedicalSpecialties.FirstOrDefaultAsync(ms => ms.Name == name);
                if (specialty == null)
                {
                    return new NotFoundObjectResult(new { message = "Specialty not found." });
                }

                _context.MedicalSpecialties.Remove(specialty);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { message = "Specialty deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteSpecialty");
                return new ObjectResult(new { message = "An error occurred while deleting specialty." })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
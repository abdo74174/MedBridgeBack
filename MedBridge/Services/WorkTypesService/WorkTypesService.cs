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
    public class WorkTypesService : IWorkTypesService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkTypesService> _logger;

        public WorkTypesService(ApplicationDbContext context, ILogger<WorkTypesService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> IsUserAdmin(string email)
        {
            var user = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null && user.IsAdmin;
        }

        public async Task<IActionResult> GetWorkTypes()
        {
            try
            {
                var workTypes = await _context.WorkType.Select(wt => wt.Name).ToListAsync();
                return new OkObjectResult(new { workTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetWorkTypes");
                return new ObjectResult(new { message = "An error occurred while retrieving work types." })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> AddWorkType(WorkTypeDto dto, string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return new UnauthorizedObjectResult(new { message = "Only admins can add work types." });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return new BadRequestObjectResult(new { message = "Work type name is required." });
                }

                var existingWorkType = await _context.WorkType.FirstOrDefaultAsync(wt => wt.Name == dto.Name);
                if (existingWorkType != null)
                {
                    return new BadRequestObjectResult(new { message = "Work type already exists." });
                }

                var workType = new WorkType { Name = dto.Name };
                _context.WorkType.Add(workType);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { message = "Work type added successfully.", workType = workType.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddWorkType");
                return new ObjectResult(new { message = "An error occurred while adding work type." })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> UpdateWorkType(string name, WorkTypeDto dto, string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return new UnauthorizedObjectResult(new { message = "Only admins can update work types." });
                }

                var workType = await _context.WorkType.FirstOrDefaultAsync(wt => wt.Name == name);
                if (workType == null)
                {
                    return new NotFoundObjectResult(new { message = "Work type not found." });
                }

                workType.Name = dto.Name;
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { message = "Work type updated successfully.", workType = workType.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateWorkType");
                return new ObjectResult(new { message = "An error occurred while updating work type." })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> DeleteWorkType(string name, string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return new UnauthorizedObjectResult(new { message = "Only admins can delete work types." });
                }

                var workType = await _context.WorkType.FirstOrDefaultAsync(wt => wt.Name == name);
                if (workType == null)
                {
                    return new NotFoundObjectResult(new { message = "Work type not found." });
                }

                _context.WorkType.Remove(workType);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { message = "Work type deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteWorkType");
                return new ObjectResult(new { message = "An error occurred while deleting work type." })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
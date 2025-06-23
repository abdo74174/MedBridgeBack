using MedBridge.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;

namespace MedBridge.Controllers.Info
{
    [ApiController]
    [Route("api/MedBridge")]
    public class WorkTypesController : Controller
    {
       

        private readonly ApplicationDbContext _context;

        private readonly ILogger<UserController> _logger;

        public WorkTypesController(ApplicationDbContext context)
        {
            _context = context;
            ILogger<WorkTypesController> logger ;
        }

    
        private async Task<bool> IsUserAdmin(string email)
        {
            var user = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null && user.IsAdmin;
        }

        [HttpGet("work-types")]
        public async Task<IActionResult> GetWorkTypes()
        {
            try
            {
                var workTypes = await _context.WorkType.Select(wt => wt.Name).ToListAsync();
                return Ok(new { workTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetWorkTypes");
                return StatusCode(500, new { message = "An error occurred while retrieving work types." });
            }
        }

        [HttpPost("work-types")]
        public async Task<IActionResult> AddWorkType([FromBody] WorkTypeDto dto, [FromQuery] string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return Unauthorized(new { message = "Only admins can add work types." });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Work type name is required." });
                }

                var existingWorkType = await _context.WorkType.FirstOrDefaultAsync(wt => wt.Name == dto.Name);
                if (existingWorkType != null)
                {
                    return BadRequest(new { message = "Work type already exists." });
                }

                var workType = new WorkType { Name = dto.Name };
                _context.WorkType.Add(workType);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Work type added successfully.", workType = workType.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddWorkType");
                return StatusCode(500, new { message = "An error occurred while adding work type." });
            }
        }

        [HttpPut("work-types/{name}")]
        public async Task<IActionResult> UpdateWorkType(string name, [FromBody] WorkTypeDto dto, [FromQuery] string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return Unauthorized(new { message = "Only admins can update work types." });
                }

                var workType = await _context.WorkType.FirstOrDefaultAsync(wt => wt.Name == name);
                if (workType == null)
                {
                    return NotFound(new { message = "Work type not found." });
                }

                workType.Name = dto.Name;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Work type updated successfully.", workType = workType.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateWorkType");
                return StatusCode(500, new { message = "An error occurred while updating work type." });
            }
        }

        [HttpDelete("work-types/{name}")]
        public async Task<IActionResult> DeleteWorkType(string name, [FromQuery] string adminEmail)
        {
            try
            {
                if (!await IsUserAdmin(adminEmail))
                {
                    return Unauthorized(new { message = "Only admins can delete work types." });
                }

                var workType = await _context.WorkType.FirstOrDefaultAsync(wt => wt.Name == name);
                if (workType == null)
                {
                    return NotFound(new { message = "Work type not found." });
                }

                _context.WorkType.Remove(workType);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Work type deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteWorkType");
                return StatusCode(500, new { message = "An error occurred while deleting work type." });
            }
        }

    }
}

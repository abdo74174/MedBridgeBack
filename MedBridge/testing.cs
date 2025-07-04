//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using MedBridge.Models;
//using MedBridge.Dtos;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;

//namespace MedBridge.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class UserController : ControllerBase
//    {
//        private readonly YourDbContext _context; // Replace with your actual DbContext
//        private readonly ILogger<UserController> _logger;

//        public UserController(YourDbContext context, ILogger<UserController> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        [HttpPut("{email}")]
     
//        [HttpGet("{email}")]
//        public async Task<IActionResult> GetUserAsync(string email)
//        {
//            try
//            {
//                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
//                if (user == null)
//                {
//                    return NotFound(new { message = "User not found." });
//                }
//                return Ok(user);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error fetching user {Email}", email);
//                return StatusCode(500, new { message = "An error occurred." });
//            }
//        }

//        [HttpGet("specialties")]
//        public IActionResult GetSpecialties()
//        {
//            var specialties = new List<string>
//            {
//                "General Internal Medicine",
//                "Cardiology",
//                "Gastroenterology & Hepatology",
//                "Nephrology & Urology",
//                "Endocrinology & Diabetes",
//                "Rheumatology & Immunology"
//            };
//            return Ok(specialties);
//        }
//    }
//}
using MedBridge.Dtos;
using MedBridge.Models.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Controllers.MessageController
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbcontext;

        public ContactUsController(ApplicationDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var contactUsMessages = await _dbcontext.ContactUs
                .Select(m => new ContactUsDto
                {
                    ProblemType = m.ProblemType,
                    Message = m.Message,
                    Email = m.Email
                })
                .ToListAsync();
            return Ok(contactUsMessages);
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] ContactUsDto contactUs)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            var message = new ContactUs
            {
                ProblemType = contactUs.ProblemType,
                Message = contactUs.Message,
                Email = contactUs.Email,
                CreatedAt = DateTime.UtcNow
            };

            await _dbcontext.ContactUs.AddAsync(message);
            await _dbcontext.SaveChangesAsync();

            return Created("", message); 
        }
    }
}
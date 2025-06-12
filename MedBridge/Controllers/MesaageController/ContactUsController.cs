using MedBridge.Dtos;
using MedBridge.Models.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System;

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
            var contactUsMessages = await _dbcontext.ContactUs.ToListAsync();
            return Ok(contactUsMessages);
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] ContactUsDto contactUs)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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

            return Ok(message);
        }
    }
}
using MedBridge.Dtos;
using MedBridge.Models.Messages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public class ContactUsService : IContactUsService
    {
        private readonly ApplicationDbContext _dbContext;

        public ContactUsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            
        }

        public async Task<IActionResult> GetAsync()
        {
            try
            {
                var contactUsMessages = await _dbContext.ContactUs
                    .Select(m => new ContactUsDto
                    {
                        ProblemType = m.ProblemType,
                        Message = m.Message,
                        Email = m.Email
                    })
                    .ToListAsync();
                return new OkObjectResult(contactUsMessages);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = "An error occurred while retrieving contact messages.", error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> AddAsync(ContactUsDto contactUs)
        {
            try
            {
                var message = new ContactUs
                {
                    ProblemType = contactUs.ProblemType,
                    Message = contactUs.Message,
                    Email = contactUs.Email,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbContext.ContactUs.AddAsync(message);
                await _dbContext.SaveChangesAsync();

                return new CreatedResult("", message);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { message = "An error occurred while adding contact message.", error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
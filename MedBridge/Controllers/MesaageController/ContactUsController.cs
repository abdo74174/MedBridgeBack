using MedBridge.Dtos;

using MedBridge.Models.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;

namespace MedBridge.Controllers.MesaageController
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbcontextt;

        public ContactUsController(ApplicationDbContext dbcontextt)
        {
            _dbcontextt = dbcontextt;
        }


        [HttpGet]
        public async Task <IActionResult> GetAsync()
        {
            var ContactUs = await _dbcontextt.ContactUs.ToListAsync();

            return Ok(ContactUs); 


        }


        [HttpPost]


        public async Task <IActionResult> AddAsync([FromForm] ContactUsDto contactus)
        {

            ContactUs message = new ContactUs
            {
                Id = contactus.Id,

                Message = contactus.Message,

                UserId = contactus.UserId
            };

           await _dbcontextt.ContactUs.AddAsync(message);

            await _dbcontextt.SaveChangesAsync();


            return Ok(message);




        }
    }
}

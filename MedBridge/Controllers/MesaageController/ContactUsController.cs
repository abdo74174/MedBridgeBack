using MedBridge.Dtos;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers.MessageController
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUsController : ControllerBase
    {
        private readonly IContactUsService _contactUsService;

        public ContactUsController(IContactUsService contactUsService)
        {
            _contactUsService = contactUsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            return await _contactUsService.GetAsync();
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

            return await _contactUsService.AddAsync(contactUs);
        }
    }
}
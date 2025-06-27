using MedBridge.Models.UserInfo;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers.Info
{
    [ApiController]
    [Route("api/MedBridge")]
    public class SpecialtiesController : ControllerBase
    {
        private readonly ISpecialtiesService _specialtiesService;

        public SpecialtiesController(ISpecialtiesService specialtiesService)
        {
            _specialtiesService = specialtiesService;
        }

        [HttpGet("specialties")]
        public async Task<IActionResult> GetSpecialties()
        {
            return await _specialtiesService.GetSpecialties();
        }

        [HttpPost("specialties")]
        public async Task<IActionResult> AddSpecialty([FromBody] MedicalSpecialtyDto dto, [FromQuery] string adminEmail)
        {
            return await _specialtiesService.AddSpecialty(dto, adminEmail);
        }

        [HttpPut("specialties/{name}")]
        public async Task<IActionResult> UpdateSpecialty(string name, [FromBody] MedicalSpecialtyDto dto, [FromQuery] string adminEmail)
        {
            return await _specialtiesService.UpdateSpecialty(name, dto, adminEmail);
        }

        [HttpDelete("specialties/{name}")]
        public async Task<IActionResult> DeleteSpecialty(string name, [FromQuery] string adminEmail)
        {
            return await _specialtiesService.DeleteSpecialty(name, adminEmail);
        }
    }
}
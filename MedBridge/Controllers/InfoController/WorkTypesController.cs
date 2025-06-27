using MedBridge.Models.UserInfo;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers.Info
{
    [ApiController]
    [Route("api/MedBridge")]
    public class WorkTypesController : ControllerBase
    {
        private readonly IWorkTypesService _workTypesService;

        public WorkTypesController(IWorkTypesService workTypesService)
        {
            _workTypesService = workTypesService;
        }

        [HttpGet("work-types")]
        public async Task<IActionResult> GetWorkTypes()
        {
            return await _workTypesService.GetWorkTypes();
        }

        [HttpPost("work-types")]
        public async Task<IActionResult> AddWorkType([FromBody] WorkTypeDto dto, [FromQuery] string adminEmail)
        {
            return await _workTypesService.AddWorkType(dto, adminEmail);
        }

        [HttpPut("work-types/{name}")]
        public async Task<IActionResult> UpdateWorkType(string name, [FromBody] WorkTypeDto dto, [FromQuery] string adminEmail)
        {
            return await _workTypesService.UpdateWorkType(name, dto, adminEmail);
        }

        [HttpDelete("work-types/{name}")]
        public async Task<IActionResult> DeleteWorkType(string name, [FromQuery] string adminEmail)
        {
            return await _workTypesService.DeleteWorkType(name, adminEmail);
        }
    }
}
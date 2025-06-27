using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            return await _adminService.GetUsers();
        }

        [HttpPut("add-admin")]
        public async Task<IActionResult> AddAdmin(int id)
        {
            return await _adminService.AddAdmin(id);
        }

        [HttpPut("delete-admin/{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            return await _adminService.DeleteAdmin(id);
        }

        [HttpPost("block-user/{id}")]
        public async Task<IActionResult> BlockUser(int id)
        {
            return await _adminService.BlockUser(id);
        }

        [HttpPost("Un_block-user/{id}")]
        public async Task<IActionResult> UnBlockUser(int id)
        {
            return await _adminService.UnBlockUser(id);
        }

        [HttpPost("Activate-user/{id}")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            return await _adminService.ActivateUser(id);
        }

        [HttpPost("deactivate-user/{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            return await _adminService.DeactivateUser(id);
        }
    }
}
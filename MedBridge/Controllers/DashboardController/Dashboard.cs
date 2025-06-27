using Microsoft.AspNetCore.Mvc;
using MedBridge.Services;
using System.Threading.Tasks;

namespace MedBridge.Controllers.Dashboard
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            return await _dashboardService.GetDashboardSummary();
        }
    }
}
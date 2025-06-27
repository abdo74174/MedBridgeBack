using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface IDashboardService
    {
        Task<IActionResult> GetDashboardSummary();
    }
}
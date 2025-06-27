using MedBridge.Models.UserInfo;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface IWorkTypesService
    {
        Task<bool> IsUserAdmin(string email);
        Task<IActionResult> GetWorkTypes();
        Task<IActionResult> AddWorkType(WorkTypeDto dto, string adminEmail);
        Task<IActionResult> UpdateWorkType(string name, WorkTypeDto dto, string adminEmail);
        Task<IActionResult> DeleteWorkType(string name, string adminEmail);
    }
}
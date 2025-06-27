using MedBridge.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface IAdminService
    {
        Task<IActionResult> GetUsers();
        Task<IActionResult> AddAdmin(int id);
        Task<IActionResult> DeleteAdmin(int id);
        Task<IActionResult> BlockUser(int id);
        Task<IActionResult> UnBlockUser(int id);
        Task<IActionResult> ActivateUser(int id);
        Task<IActionResult> DeactivateUser(int id);
    }
}
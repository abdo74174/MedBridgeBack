using MedBridge.Models.UserInfo;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface ISpecialtiesService
    {
        Task<bool> IsUserAdmin(string email);
        Task<IActionResult> GetSpecialties();
        Task<IActionResult> AddSpecialty(MedicalSpecialtyDto dto, string adminEmail);
        Task<IActionResult> UpdateSpecialty(string name, MedicalSpecialtyDto dto, string adminEmail);
        Task<IActionResult> DeleteSpecialty(string name, string adminEmail);
    }
}
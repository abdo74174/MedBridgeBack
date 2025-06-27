using MedBridge.Models.ProductModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface ICategoryService
    {
        Task<IActionResult> CreateAsync(CategoryDto dto);
        Task<IActionResult> GetAllAsync();
        Task<IActionResult> GetByIdAsync(int id);
        Task<IActionResult> UpdateAsync(int id, CategoryDto dto);
        Task<IActionResult> DeleteAsync(int id);
    }
}
using MedBridge.Dtos.Product;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface ISubcategoryService
    {
        Task<IActionResult> CreateAsync(subCategoriesDto dto);
        Task<IActionResult> GetAllAsync();
        Task<IActionResult> GetByIdAsync(int id);
        Task<IActionResult> UpdateAsync(int id, subCategoriesDto dto);
        Task<IActionResult> DeleteAsync(int id);
    }
}
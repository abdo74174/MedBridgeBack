using MedBridge.Dtos.ProductADD;
using MedBridge.Dtos.ProductDto;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface IProductService
    {
        Task<IActionResult> CreateAsync(ProductADDDto dto);
        Task<IActionResult> GetAllAsync();
        Task<IActionResult> GetPendingProductsAsync();
        Task<IActionResult> GetByIdAsync(int id);
        Task<IActionResult> UpdateAsync(int id, ProductADDDto dto);
        Task<IActionResult> ApproveProductAsync(int id, ProductApprovalDto dto);
        Task<IActionResult> DeleteAsync(int id);
        IActionResult GetRecommendations(int productId, int topN);
        IActionResult GetImage(string fileName);
    }
}
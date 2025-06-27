using MedBridge.Dtos;
using MedBridge.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface ICartServicee
    {
        Task<int?> GetUserId(Microsoft.AspNetCore.Http.HttpRequest request);
        Task<IActionResult> AddToCart(CartItemDto model, int? userId);
        Task<IActionResult> GetCart(int? userId);
        Task<IActionResult> DeleteFromCart(int productId, int? userId);
        Task<IActionResult> UpdateQuantity(CartItemDto model, int? userId);
        Task<IActionResult> ClearCart(int? userId);
        object SerializeCart(CartModel cart);
    }
}
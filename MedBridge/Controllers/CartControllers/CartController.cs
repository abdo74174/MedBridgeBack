using MedBridge.Dtos;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [Route("api/cart")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartServicee _cartService;

        public CartController(ICartServicee cartService)
        {
            _cartService = cartService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromForm] CartItemDto model)
        {
            var userId = await _cartService.GetUserId(Request);
            return await _cartService.AddToCart(model, userId);
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = await _cartService.GetUserId(Request);
            return await _cartService.GetCart(userId);
        }

        [HttpDelete("delete/{productId}")]
        public async Task<IActionResult> DeleteFromCart(int productId)
        {
            var userId = await _cartService.GetUserId(Request);
            return await _cartService.DeleteFromCart(productId, userId);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartItemDto model)
        {
            var userId = await _cartService.GetUserId(Request);
            return await _cartService.UpdateQuantity(model, userId);
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = await _cartService.GetUserId(Request);
            return await _cartService.ClearCart(userId);
        }
    }
}
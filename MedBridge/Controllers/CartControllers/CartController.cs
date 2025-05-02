using MedBridge.Dtos;
using MedBridge.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [Route("api/cart")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;

        public CartController(ApplicationDbContext context, CartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        private async Task<int?> GetUserId()
        {
            var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(userIdStr))
            {
                Console.WriteLine("Error: X-User-Id header is missing");
                return null;
            }

            if (!int.TryParse(userIdStr, out int userId))
            {
                Console.WriteLine($"Error: Invalid user_id format: {userIdStr}. Must be a numeric value.");
                return null;
            }

            // Validate user_id exists in Users table
            var userExists = await _context.users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                Console.WriteLine($"Error: User not found for user_id: {userId}");
                return null;
            }

            return userId;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromForm] CartItemDto model)
        {
            try
            {
                if (model.Quantity <= 0)
                    return BadRequest("Quantity must be greater than 0.");

                int? userId = await GetUserId();
                if (userId == null)
                    return Unauthorized("Invalid user.");

                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                    return NotFound("Product not found.");

                if (product.StockQuantity <= 0)
                    return BadRequest("Product is out of stock.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null)
                {
                    cart = new CartModel { UserId = userId.ToString(), CartItems = new List<CartItem>() };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ProductId);
                if (cartItem == null)
                {
                    cartItem = new CartItem { ProductId = model.ProductId, Quantity = model.Quantity };
                    cart.CartItems.Add(cartItem);
                }
                else
                {
                    cartItem.Quantity += model.Quantity;
                }

                await _context.SaveChangesAsync();

                var serializedCart = _cartService.SerializeCart(cart);

                return Ok(new { Message = "Product added to cart", Cart = serializedCart });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddToCart: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while adding product to cart.", Error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                int? userId = await GetUserId();
                if (userId == null)
                    return Unauthorized("Invalid user.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null || !cart.CartItems.Any())
                    return Ok(new { Message = "Cart is empty" });

                var serializedCart = _cartService.SerializeCart(cart);

                return Ok(serializedCart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCart: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while fetching cart.", Error = ex.Message });
            }
        }

        [HttpDelete("delete/{productId}")]
        public async Task<IActionResult> DeleteFromCart(int productId)
        {
            try
            {
                int? userId = await GetUserId();
                if (userId == null)
                    return Unauthorized("Invalid user.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null)
                    return NotFound("Cart not found.");

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (cartItem == null)
                    return NotFound("Product not found in cart.");

                cart.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                var serializedCart = _cartService.SerializeCart(cart);

                return Ok(new { Message = "Product removed from cart", Cart = serializedCart });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteFromCart: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while deleting product from cart.", Error = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartItemDto model)
        {
            try
            {
                Console.WriteLine($"ProductId: {model.ProductId}, Quantity: {model.Quantity}");

                if (model.Quantity <= 0)
                    return BadRequest("Quantity must be greater than 0.");

                int? userId = await GetUserId();
                if (userId == null)
                    return Unauthorized("Invalid user.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null)
                    return NotFound("Cart not found.");

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ProductId);
                if (cartItem == null)
                    return NotFound("Product not found in cart.");

                cartItem.Quantity = model.Quantity;
                await _context.SaveChangesAsync();

                var serializedCart = _cartService.SerializeCart(cart);

                return Ok(new { Message = "Quantity updated", Cart = serializedCart });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateQuantity: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while updating cart.", Error = ex.Message });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                int? userId = await GetUserId();
                if (userId == null)
                    return Unauthorized("Invalid user.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null)
                    return NotFound("Cart not found.");

                cart.CartItems.Clear();
                await _context.SaveChangesAsync();

                var serializedCart = _cartService.SerializeCart(cart);

                return Ok(new { Message = "Cart cleared", Cart = serializedCart });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ClearCart: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while clearing cart.", Error = ex.Message });
            }
        }
    }
}
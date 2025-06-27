using MedBridge.Dtos;
using MedBridge.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public class CartServicee : ICartServicee
    {
        private readonly ApplicationDbContext _context;

        public CartServicee(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int?> GetUserId(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            var userIdStr = request.Headers["X-User-Id"].FirstOrDefault();
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

            var userExists = await _context.users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                Console.WriteLine($"Error: User not found for user_id: {userId}");
                return null;
            }

            return userId;
        }

        public async Task<IActionResult> AddToCart(CartItemDto model, int? userId)
        {
            try
            {
                if (model.Quantity <= 0)
                    return new BadRequestObjectResult("Quantity must be greater than 0.");

                if (userId == null)
                    return new UnauthorizedObjectResult("Invalid user.");

                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                    return new NotFoundObjectResult("Product not found.");

                if (product.StockQuantity <= 0)
                    return new BadRequestObjectResult("Product is out of stock.");

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

                var serializedCart = SerializeCart(cart);

                return new OkObjectResult(new { Message = "Product added to cart", Cart = serializedCart });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddToCart: {ex.Message}");
                return new ObjectResult(new { Message = "An error occurred while adding product to cart.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> GetCart(int? userId)
        {
            try
            {
                if (userId == null)
                    return new UnauthorizedObjectResult("Invalid user.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null || !cart.CartItems.Any())
                    return new OkObjectResult(new { Message = "Cart is empty" });

                var serializedCart = SerializeCart(cart);

                return new OkObjectResult(serializedCart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCart: {ex.Message}");
                return new ObjectResult(new { Message = "An error occurred while fetching cart.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> DeleteFromCart(int productId, int? userId)
        {
            try
            {
                if (userId == null)
                    return new UnauthorizedObjectResult("Invalid user.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null)
                    return new NotFoundObjectResult("Cart not found.");

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (cartItem == null)
                    return new NotFoundObjectResult("Product not found in cart.");

                cart.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                var serializedCart = SerializeCart(cart);

                return new OkObjectResult(new { Message = "Product removed from cart", Cart = serializedCart });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteFromCart: {ex.Message}");
                return new ObjectResult(new { Message = "An error occurred while deleting product from cart.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> UpdateQuantity(CartItemDto model, int? userId)
        {
            try
            {
                Console.WriteLine($"ProductId: {model.ProductId}, Quantity: {model.Quantity}");

                if (model.Quantity <= 0)
                    return new BadRequestObjectResult("Quantity must be greater than 0.");

                if (userId == null)
                    return new UnauthorizedObjectResult("Invalid user.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null)
                    return new NotFoundObjectResult("Cart not found.");

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == model.ProductId);
                if (cartItem == null)
                    return new NotFoundObjectResult("Product not found in cart.");

                cartItem.Quantity = model.Quantity;
                await _context.SaveChangesAsync();

                var serializedCart = SerializeCart(cart);

                return new OkObjectResult(new { Message = "Quantity updated", Cart = serializedCart });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateQuantity: {ex.Message}");
                return new ObjectResult(new { Message = "An error occurred while updating cart.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> ClearCart(int? userId)
        {
            try
            {
                if (userId == null)
                    return new UnauthorizedObjectResult("Invalid user.");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId.ToString());

                if (cart == null)
                    return new NotFoundObjectResult("Cart not found.");

                cart.CartItems.Clear();
                await _context.SaveChangesAsync();

                var serializedCart = SerializeCart(cart);

                return new OkObjectResult(new { Message = "Cart cleared", Cart = serializedCart });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ClearCart: {ex.Message}");
                return new ObjectResult(new { Message = "An error occurred while clearing cart.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public object SerializeCart(CartModel cart)
        {
            return new
            {
                cart.UserId,
                CartItems = cart.CartItems.Select(ci => new
                {
                    ci.ProductId,
                    ci.Quantity,
                    Product = ci.Product != null ? new
                    {
                        ci.Product.ProductId,
                        ci.Product.Name,
                        ci.Product.Description,
                        ci.Product.Price
                    } : null
                }).ToList()
            };
        }
    }
}
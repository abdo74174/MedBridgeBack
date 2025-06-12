using MedBridge.Dtos.OrderDtos;
using MedBridge.Models;
using MedBridge.Models.OrderModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;

namespace MedBridge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                var user = await _context.users.FindAsync(dto.UserId);
                if (user == null)
                    return NotFound("User not found.");

                var productIds = dto.Items.Select(i => i.ProductId).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId) && !p.isdeleted)
                    .ToListAsync();

                if (products.Count != dto.Items.Count)
                    return NotFound("One or more products not found.");

                foreach (var item in dto.Items)
                {
                    var product = products.First(p => p.ProductId == item.ProductId);
                    if (product.StockQuantity < item.Quantity)
                        return BadRequest($"Not enough stock for product: {product.Name}");
                }

                var totalPrice = dto.Items.Sum(item =>
                {
                    var product = products.First(p => p.ProductId == item.ProductId);
                    return product.Price * item.Quantity;
                });

                var order = new Order
                {
                    UserId = dto.UserId,
                    TotalPrice = totalPrice,
                    IsDeleted = false,
                    OrderItems = dto.Items.Select(item =>
                    {
                        var product = products.First(p => p.ProductId == item.ProductId);
                        return new OrderItem
                        {
                            ProductId = product.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = product.Price
                        };
                    }).ToList()
                };

                foreach (var item in dto.Items)
                {
                    var product = products.First(p => p.ProductId == item.ProductId);
                    product.StockQuantity -= item.Quantity;
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Ok(new { OrderId = order.OrderId, Message = "Order created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromQuery] string status)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                    return BadRequest("Invalid status value.");

                var order = await _context.Orders.FindAsync(id);
                if (order == null || order.IsDeleted)
                    return NotFound("Order not found.");

                order.Status = orderStatus;
                await _context.SaveChangesAsync();

                return Ok($"Order status updated to {status}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDetailsDto>> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id && !o.IsDeleted);

                if (order == null)
                    return NotFound("Order not found.");

                var dto = new OrderDetailsDto
                {
                    OrderId = order.OrderId,
                    UserName = order.User?.Name ?? "Unknown",
                    OrderDate = order.OrderDate,
                    Status = order.Status.ToString(),
                    TotalPrice = order.TotalPrice,
                    Items = order.OrderItems?.Select(i => new OrderDetailsItemDto
                    {
                        ProductName = i.Product?.Name ?? "Unknown",
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList() ?? new List<OrderDetailsItemDto>()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<OrderDetailsDto>>> GetAllOrders()
        //{
        //    try
        //    {
        //        var orders = await _context.Orders
        //            .Include(o => o.User)
        //            .Include(o => o.OrderItems)
        //                .ThenInclude(i => i.Product)
        //            .Where(o => !o.IsDeleted)
        //            .ToListAsync();

        //        var dtos = orders.Select(order => new OrderDetailsDto
        //        {
        //            OrderId = order.OrderId,
        //            UserName = order.User?.Name ?? "Unknown",
        //            OrderDate = order.OrderDate,
        //            Status = order.Status.ToString(),
        //            TotalPrice = order.TotalPrice,
        //            Items = order.OrderItems?.Select(i => new OrderDetailsItemDto
        //            {
        //                ProductName = i.Product?.Name ?? "Unknown",
        //                Quantity = i.Quantity,
        //                UnitPrice = i.UnitPrice
        //            }).ToList() ?? new List<OrderDetailsItemDto>()
        //        }).ToList();

        //        return Ok(dtos);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error: {ex.Message}");
        //    }
        //}
        [HttpGet]
        public IActionResult GetOrders()
        {
            var orders = _context.Orders
                .Include(o => o.User) // Ensure User is included if needed
                .Select(static o => new
                {
                    orderId = o.OrderId,
                    userId = o.UserId, // Explicitly include UserId
                    userName = o.User.Name ?? "Unknown" ,
                    orderDate = o.OrderDate,
                    status = o.Status.ToString(),
                    totalPrice = o.TotalPrice,
                    items = o.OrderItems.Select(i => new
                    {
                        productName = i.Product.Name ?? "Unknown",
                        quantity = i.Quantity,
                        unitPrice = i.UnitPrice
                    })
                })
                .ToList();
            return Ok(orders);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id && !o.IsDeleted);

                if (order == null)
                    return NotFound("Order not found.");

                order.IsDeleted = true;

                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok("Order deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
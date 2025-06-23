using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MedBridge.Dtos.OrderDtos;
using MedBridge.Models;
using MedBridge.Models.OrderModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using static Order;

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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
                    Address = dto.Address,
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        [HttpPut("{id}/delivery-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateOrderStatusByDeliveryPerson(int id, [FromQuery] int deliveryPersonId, [FromQuery] string status)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                    return BadRequest("Invalid status value.");

                var order = await _context.Orders.FindAsync(id);
                if (order == null || order.IsDeleted)
                    return NotFound("Order not found.");

                if (order.DeliveryPersonId != deliveryPersonId)
                    return StatusCode(403, "You are not assigned to this order.");

                if (orderStatus != OrderStatus.Shipped && orderStatus != OrderStatus.Delivered && orderStatus != OrderStatus.Cancelled)
                    return BadRequest("Delivery person can only set status to Shipped, Delivered, or Cancelled.");

                if (orderStatus == OrderStatus.Shipped)
                {
                    order.DeliveryPersonConfirmedShipped = true;

                    if (order.UserConfirmedShipped)
                    {
                        order.Status = OrderStatus.Shipped;
                    }
                    else
                    {
                        order.Status = OrderStatus.AwaitingUserConfirmation;
                    }
                }
                else
                {
                    order.Status = orderStatus;
                    order.UserConfirmedShipped = false;
                    order.DeliveryPersonConfirmedShipped = false;

                    if (orderStatus == OrderStatus.Delivered || orderStatus == OrderStatus.Cancelled)
                    {
                        var deliveryPerson = await _context.DeliveryPersons.FirstOrDefaultAsync(dp => dp.userId == deliveryPersonId);
                        if (deliveryPerson != null)
                        {
                            deliveryPerson.IsAvailable = true;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok($"Order status updated to {order.Status} by delivery person.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("{id}/user-confirm-shipped")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UserConfirmShipped(int id, [FromQuery] int userId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null || order.IsDeleted)
                    return NotFound("Order not found.");

                if (order.UserId != userId)
                    return StatusCode(403, "You are not authorized to confirm this order.");

                if (order.Status != OrderStatus.AwaitingUserConfirmation)
                    return BadRequest("Order is not awaiting user confirmation.");

                order.UserConfirmedShipped = true;

                if (order.DeliveryPersonConfirmedShipped)
                {
                    order.Status = OrderStatus.Shipped;
                }
                else
                {
                    order.Status = OrderStatus.AwaitingDeliveryConfirmation;
                }

                await _context.SaveChangesAsync();

                return Ok($"User confirmed shipped status. Order status is now {order.Status}.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("{id}/assign-delivery")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignDeliveryPerson(int id, [FromQuery] int deliveryPersonId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null || order.IsDeleted)
                    return NotFound("Order not found.");

                var deliveryPerson = await _context.DeliveryPersons.FirstOrDefaultAsync(dp => dp.userId == deliveryPersonId);
                if (deliveryPerson == null || deliveryPerson.IsAvailable != true)
                    return BadRequest("Delivery person not available or not found.");

                if (string.Compare(deliveryPerson.Address, order.Address, StringComparison.OrdinalIgnoreCase) != 0)
                    return BadRequest("Delivery person address does not match order address.");

                order.DeliveryPersonId = deliveryPersonId;
                order.Status = OrderStatus.Assigned;
                deliveryPerson.IsAvailable = false;
                await _context.SaveChangesAsync();

                return Ok($"Order assigned to delivery person ID: {deliveryPersonId}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("delivery/{deliveryPersonId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderDetailsDto>>> GetOrdersByDeliveryPerson(int deliveryPersonId)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .Where(o => o.DeliveryPersonId == deliveryPersonId && !o.IsDeleted && o.Status == OrderStatus.Assigned)
                    .ToListAsync();

                var dtos = orders.Select(order => new OrderDetailsDto
                {
                    OrderId = order.OrderId,
                    UserName = order.User?.Name ?? "Unknown",
                    Address = order.Address,
                    OrderDate = order.OrderDate,
                    Status = order.Status.ToString(),
                    TotalPrice = order.TotalPrice,
                    UserConfirmedShipped = order.UserConfirmedShipped,
                    DeliveryPersonConfirmedShipped = order.DeliveryPersonConfirmedShipped,
                    Items = order.OrderItems?.Select(i => new OrderDetailsItemDto
                    {
                        ProductName = i.Product?.Name ?? "Unknown",
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList() ?? new List<OrderDetailsItemDto>()
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("user/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderDetailsDto>>> GetOrdersByUser(int userId)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .Where(o => o.UserId == userId && !o.IsDeleted)
                    .ToListAsync();

                var dtos = orders.Select(order => new OrderDetailsDto
                {
                    OrderId = order.OrderId,
                    UserName = order.User?.Name ?? "Unknown",
                    Address = order.Address,
                    OrderDate = order.OrderDate,
                    Status = order.Status.ToString(),
                    TotalPrice = order.TotalPrice,
                    UserConfirmedShipped = order.UserConfirmedShipped,
                    DeliveryPersonConfirmedShipped = order.DeliveryPersonConfirmedShipped,
                    Items = order.OrderItems?.Select(i => new OrderDetailsItemDto
                    {
                        ProductName = i.Product?.Name ?? "Unknown",
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList() ?? new List<OrderDetailsItemDto>()
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetOrders()
        {
            try
            {
                var orders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.DeliveryPerson)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Select(o => new
                    {
                        orderId = o.OrderId,
                        userId = o.UserId,
                        userName = o.User != null ? o.User.Name : "Unknown",
                        deliveryPersonId = o.DeliveryPersonId,
                        address = o.Address,
                        deliveryPersonName = o.DeliveryPerson != null ? o.DeliveryPerson.Name : "Unassigned",
                        orderDate = o.OrderDate,
                        status = o.Status.ToString(),
                        totalPrice = o.TotalPrice,
                        userConfirmedShipped = o.UserConfirmedShipped,
                        deliveryPersonConfirmedShipped = o.DeliveryPersonConfirmedShipped,
                        items = o.OrderItems.Select(i => new
                        {
                            productName = i.Product != null ? i.Product.Name : "Unknown",
                            quantity = i.Quantity,
                            unitPrice = i.UnitPrice
                        })
                    })
                    .ToList();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id && !o.IsDeleted);

                if (order == null)
                    return NotFound("Order not found.");

                if (order.DeliveryPersonId != null)
                {
                    var deliveryPerson = await _context.DeliveryPersons.FirstOrDefaultAsync(dp => dp.userId == order.DeliveryPersonId);
                    if (deliveryPerson != null)
                        deliveryPerson.IsAvailable = true;
                }

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

        [HttpGet("delivery-persons")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DeliveryPersonDto>>> GetAvailableDeliveryPersons()
        {
            try
            {
                var deliveryPersons = await _context.DeliveryPersons
                    .Where(dp => dp.IsAvailable == true && dp.RequestStatus == "Approved")
                    .Select(dp => new DeliveryPersonDto
                    {
                        Id = dp.userId,
                        Name = dp.Name,
                        Email = dp.Email,
                        Phone = dp.Phone
                    })
                    .ToListAsync();

                return Ok(deliveryPersons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }

    public class DeliveryPersonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class OrderDetailsDto
    {
        public int OrderId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public bool UserConfirmedShipped { get; set; }
        public bool DeliveryPersonConfirmedShipped { get; set; }
        public List<OrderDetailsItemDto> Items { get; set; } = new List<OrderDetailsItemDto>();
    }

    public class OrderDetailsItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
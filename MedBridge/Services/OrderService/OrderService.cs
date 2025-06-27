using MedBridge.Dtos.DeliveryPersonRequestDto;
using MedBridge.Dtos.OrderDtos;
using MedBridge.Models;
using MedBridge.Models.OrderModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApi.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Order;

namespace MedBridge.Services
{
    public class OrderServicee : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderServicee> _logger;

        public OrderServicee(ApplicationDbContext context, ILogger<OrderServicee> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
        {
            try
            {
                var user = await _context.users.FindAsync(dto.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for UserId: {UserId}", dto.UserId);
                    return new NotFoundObjectResult("User not found.");
                }

                var productIds = dto.Items.Select(i => i.ProductId).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId) && !p.isdeleted)
                    .ToListAsync();

                if (products.Count != dto.Items.Count)
                {
                    _logger.LogWarning("One or more products not found for order creation");
                    return new NotFoundObjectResult("One or more products not found.");
                }

                foreach (var item in dto.Items)
                {
                    var product = products.First(p => p.ProductId == item.ProductId);
                    if (product.StockQuantity < item.Quantity)
                    {
                        _logger.LogWarning("Not enough stock for product: {ProductName}", product.Name);
                        return new BadRequestObjectResult($"Not enough stock for product: {product.Name}");
                    }
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

                _logger.LogInformation("Order created successfully with OrderId: {OrderId}", order.OrderId);
                return new OkObjectResult(new { OrderId = order.OrderId, Message = "Order created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for UserId: {UserId}", dto.UserId);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    _logger.LogWarning("Invalid status value: {Status}", status);
                    return new BadRequestObjectResult("Invalid status value.");
                }

                var order = await _context.Orders.FindAsync(id);
                if (order == null || order.IsDeleted)
                {
                    _logger.LogWarning("Order not found for OrderId: {OrderId}", id);
                    return new NotFoundObjectResult("Order not found.");
                }

                order.Status = orderStatus;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order status updated to {Status} for OrderId: {OrderId}", status, id);
                return new OkObjectResult($"Order status updated to {status}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for OrderId: {OrderId}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UpdateOrderStatusByDeliveryPerson(int id, int deliveryPersonId, string status)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    _logger.LogWarning("Invalid status value: {Status}", status);
                    return new BadRequestObjectResult("Invalid status value.");
                }

                var order = await _context.Orders.FindAsync(id);
                if (order == null || order.IsDeleted)
                {
                    _logger.LogWarning("Order not found for OrderId: {OrderId}", id);
                    return new NotFoundObjectResult("Order not found.");
                }

                if (order.DeliveryPersonId != deliveryPersonId)
                {
                    _logger.LogWarning("Delivery person {DeliveryPersonId} not assigned to OrderId: {OrderId}", deliveryPersonId, id);
                    return new ObjectResult("You are not assigned to this order.") { StatusCode = 403 };
                }

                if (orderStatus != OrderStatus.Shipped && orderStatus != OrderStatus.Delivered && orderStatus != OrderStatus.Cancelled)
                {
                    _logger.LogWarning("Invalid status for delivery person: {Status}", status);
                    return new BadRequestObjectResult("Delivery person can only set status to Shipped, Delivered, or Cancelled.");
                }

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
                        var deliveryPerson = await _context.DeliveryPersons.FirstOrDefaultAsync(dp => dp.UserId == deliveryPersonId);
                        if (deliveryPerson != null)
                        {
                            deliveryPerson.IsAvailable = true;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order status updated to {Status} by delivery person {DeliveryPersonId} for OrderId: {OrderId}", order.Status, deliveryPersonId, id);
                return new OkObjectResult($"Order status updated to {order.Status} by delivery person.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status by delivery person {DeliveryPersonId} for OrderId: {OrderId}", deliveryPersonId, id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UserConfirmShipped(int id, int userId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null || order.IsDeleted)
                {
                    _logger.LogWarning("Order not found for OrderId: {OrderId}", id);
                    return new NotFoundObjectResult("Order not found.");
                }

                if (order.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} not authorized to confirm OrderId: {OrderId}", userId, id);
                    return new ObjectResult("You are not authorized to confirm this order.") { StatusCode = 403 };
                }

                if (order.Status != OrderStatus.AwaitingUserConfirmation)
                {
                    _logger.LogWarning("Order {OrderId} is not awaiting user confirmation", id);
                    return new BadRequestObjectResult("Order is not awaiting user confirmation.");
                }

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

                _logger.LogInformation("User {UserId} confirmed shipped status for OrderId: {OrderId}, new status: {Status}", userId, id, order.Status);
                return new OkObjectResult($"User confirmed shipped status. Order status is now {order.Status}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming shipped status by user {UserId} for OrderId: {OrderId}", userId, id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> AssignDeliveryPerson(int id, int deliveryPersonId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null || order.IsDeleted)
                {
                    _logger.LogWarning("Order not found for OrderId: {OrderId}", id);
                    return new NotFoundObjectResult("Order not found.");
                }

                var deliveryPerson = await _context.DeliveryPersons.FirstOrDefaultAsync(dp => dp.UserId == deliveryPersonId);
                if (deliveryPerson == null || deliveryPerson.IsAvailable != true)
                {
                    _logger.LogWarning("Delivery person not available or not found for DeliveryPersonId: {DeliveryPersonId}", deliveryPersonId);
                    return new BadRequestObjectResult("Delivery person not available or not found.");
                }

                if (string.Compare(deliveryPerson.Address, order.Address, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    _logger.LogWarning("Delivery person address does not match order address for OrderId: {OrderId}", id);
                    return new BadRequestObjectResult("Delivery person address does not match order address.");
                }

                order.DeliveryPersonId = deliveryPersonId;
                order.Status = OrderStatus.Assigned;
                deliveryPerson.IsAvailable = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} assigned to delivery person {DeliveryPersonId}", id, deliveryPersonId);
                return new OkObjectResult($"Order assigned to delivery person ID: {deliveryPersonId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning delivery person {DeliveryPersonId} to OrderId: {OrderId}", deliveryPersonId, id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetOrdersByDeliveryPerson(int deliveryPersonId)
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

                _logger.LogInformation("Retrieved {Count} orders for delivery person {DeliveryPersonId}", dtos.Count, deliveryPersonId);
                return new OkObjectResult(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for delivery person {DeliveryPersonId}", deliveryPersonId);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetOrdersByUser(int userId)
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

                _logger.LogInformation("Retrieved {Count} orders for user {UserId}", dtos.Count, userId);
                return new OkObjectResult(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for user {UserId}", userId);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

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

                _logger.LogInformation("Retrieved {Count} orders", orders.Count);
                return new OkObjectResult(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders");
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == id && !o.IsDeleted);

                if (order == null)
                {
                    _logger.LogWarning("Order not found for OrderId: {OrderId}", id);
                    return new NotFoundObjectResult("Order not found.");
                }

                if (order.DeliveryPersonId != null)
                {
                    var deliveryPerson = await _context.DeliveryPersons.FirstOrDefaultAsync(dp => dp.UserId == order.DeliveryPersonId);
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

                _logger.LogInformation("Order deleted successfully for OrderId: {OrderId}", id);
                return new OkObjectResult("Order deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order for OrderId: {OrderId}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetAvailableDeliveryPersons()
        {
            try
            {
                var deliveryPersons = await _context.DeliveryPersons
                    .Where(dp => dp.IsAvailable == true && dp.RequestStatus == "Approved")
                    .Select(dp => new DeliveryPersonDto
                    {
                        Id = dp.UserId,
                        Name = dp.Name,
                        Email = dp.Email,
                        Phone = dp.Phone
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} available delivery persons", deliveryPersons.Count);
                return new OkObjectResult(deliveryPersons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available delivery persons");
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }
    }
}
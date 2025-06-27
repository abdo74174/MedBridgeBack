using GraduationProject.Core.Dtos;
using GraduationProject.Core.Entities;
using MedBridge.Models.OrderModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApi.models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Order;

namespace GraduationProject.Core.Services
{
    public class DeliveryPersonAdminService : IDeliveryPersonAdminService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DeliveryPersonAdminService> _logger;

        public DeliveryPersonAdminService(ApplicationDbContext dbContext, ILogger<DeliveryPersonAdminService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> GetDeliveryPersonRequests()
        {
            try
            {
                var requests = await _dbContext.DeliveryPersons
                    .Select(dp => new DeliveryPersonRequestAdminDto
                    {
                        Id = dp.Id,
                        Name = dp.Name,
                        Email = dp.Email,
                        Phone = dp.Phone,
                        Address = dp.Address,
                        CardNumber = dp.CardNumber,
                        RequestStatus = dp.RequestStatus,
                        IsAvailable = dp.IsAvailable,
                        CreatedAt = dp.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} delivery person requests", requests.Count);
                return new OkObjectResult(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch delivery person requests");
                return new BadRequestObjectResult($"Failed to fetch requests: {ex.Message}");
            }
        }

        public async Task<IActionResult> HandleDeliveryPersonRequest(int requestId, string action)
        {
            try
            {
                var deliveryPerson = await _dbContext.DeliveryPersons.FirstOrDefaultAsync(dp => dp.Id == requestId);
                if (deliveryPerson == null)
                {
                    _logger.LogWarning("Delivery person not found for requestId: {RequestId}", requestId);
                    return new BadRequestObjectResult($"Delivery person with ID {requestId} not found.");
                }

                switch (action.ToLower())
                {
                    case "approve":
                        deliveryPerson.RequestStatus = "Approved";
                        deliveryPerson.IsAvailable = true;
                        break;
                    case "reject":
                        deliveryPerson.RequestStatus = "Rejected";
                        deliveryPerson.IsAvailable = false;
                        break;
                    case "pending":
                        deliveryPerson.RequestStatus = "Pending";
                        deliveryPerson.IsAvailable = false;
                        break;
                    default:
                        _logger.LogWarning("Invalid action: {Action} for requestId: {RequestId}", action, requestId);
                        return new BadRequestObjectResult($"Invalid action: {action}.");
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Delivery person request {Action} for requestId: {RequestId}", action, requestId);
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling delivery person request for requestId: {RequestId}", requestId);
                return new BadRequestObjectResult($"Failed to handle request: {ex.Message}");
            }
        }

        public async Task<IActionResult> GetAvailableDeliveryPersons(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                _logger.LogWarning("Address is required for fetching available delivery persons.");
                return new BadRequestObjectResult("Address is required.");
            }

            try
            {
                var deliveryPersons = await _dbContext.DeliveryPersons
                    .Where(dp => dp.RequestStatus == "Approved" && dp.IsAvailable == true && dp.Address.ToLower() == address.ToLower())
                    .Select(dp => new DeliveryPersonRequestAdminDto
                    {
                        Id = dp.Id,
                        Name = dp.Name,
                        Email = dp.Email,
                        Phone = dp.Phone,
                        Address = dp.Address,
                        CardNumber = dp.CardNumber,
                        RequestStatus = dp.RequestStatus,
                        IsAvailable = dp.IsAvailable,
                        CreatedAt = dp.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} available delivery persons for address: {Address}", deliveryPersons.Count, address);
                return new OkObjectResult(deliveryPersons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch available delivery persons for address: {Address}", address);
                return new BadRequestObjectResult($"Failed to fetch delivery persons: {ex.Message}");
            }
        }

        public async Task<IActionResult> AssignOrder(AssignOrderDto assignOrderDto)
        {
            try
            {
                var order = await _dbContext.Orders.FindAsync(assignOrderDto.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found for orderId: {OrderId}", assignOrderDto.OrderId);
                    return new BadRequestObjectResult($"Order with ID {assignOrderDto.OrderId} not found.");
                }

                var deliveryPerson = await _dbContext.DeliveryPersons.FirstOrDefaultAsync(dp => dp.Id == assignOrderDto.DeliveryPersonId);
                if (deliveryPerson == null)
                {
                    _logger.LogWarning("Delivery person not found for deliveryPersonId: {DeliveryPersonId}", assignOrderDto.DeliveryPersonId);
                    return new BadRequestObjectResult($"Delivery person with ID {assignOrderDto.DeliveryPersonId} not found.");
                }

                if (deliveryPerson.RequestStatus != "Approved" || deliveryPerson.IsAvailable != true)
                {
                    _logger.LogWarning("Delivery person is not approved or available for deliveryPersonId: {DeliveryPersonId}", assignOrderDto.DeliveryPersonId);
                    return new BadRequestObjectResult("Delivery person is not approved or available.");
                }

                order.DeliveryPersonId = assignOrderDto.DeliveryPersonId;
                order.Status = OrderStatus.Assigned;
                deliveryPerson.IsAvailable = false;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} assigned to delivery person {DeliveryPersonId}", assignOrderDto.OrderId, assignOrderDto.DeliveryPersonId);
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign order {OrderId} to delivery person {DeliveryPersonId}", assignOrderDto.OrderId, assignOrderDto.DeliveryPersonId);
                return new BadRequestObjectResult($"Failed to assign order: {ex.Message}");
            }
        }

        public IActionResult GetOrderStatistics()
        {
            try
            {
                var statistics = _dbContext.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToDictionary(k => k.Status, v => v.Count);

                _logger.LogInformation("Fetched order statistics");
                return new OkObjectResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch order statistics");
                return new BadRequestObjectResult($"Failed to fetch statistics: {ex.Message}");
            }
        }

        public IActionResult GetOrders()
        {
            try
            {
                var orders = _dbContext.Orders
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
                        items = o.OrderItems.Select(i => new
                        {
                            productName = i.Product != null ? i.Product.Name : "Unknown",
                            quantity = i.Quantity,
                            unitPrice = i.UnitPrice
                        })
                    })
                    .ToList();

                _logger.LogInformation("Fetched {Count} orders", orders.Count);
                return new OkObjectResult(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch orders");
                return new BadRequestObjectResult($"Failed to fetch orders: {ex.Message}");
            }
        }
    }
}
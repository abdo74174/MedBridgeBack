using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GraduationProject.Core.Dtos;
using Microsoft.AspNetCore.Http;
using MedBridge.Models.OrderModels;
using GraduationProject.Core.Entities;
using MoviesApi.models;

namespace GraduationProject.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryPersonAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DeliveryPersonAdminController> _logger;

        public DeliveryPersonAdminController(ApplicationDbContext dbContext, ILogger<DeliveryPersonAdminController> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("requests")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
                        IsAvailable = dp.IsAvailable ?? false,
                        CreatedAt = dp.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} delivery person requests", requests.Count);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch delivery person requests");
                return BadRequest($"Failed to fetch requests: {ex.Message}");
            }
        }

        [HttpPut("request/{requestId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandleDeliveryPersonRequest(int requestId, [FromQuery] string action)
        {
            try
            {
                var deliveryPerson = await _dbContext.DeliveryPersons.FirstOrDefaultAsync(dp => dp.Id == requestId);
                if (deliveryPerson == null)
                {
                    _logger.LogWarning("Delivery person not found for requestId: {RequestId}", requestId);
                    return BadRequest($"Delivery person with ID {requestId} not found.");
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
                        return BadRequest($"Invalid action: {action}.");
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Delivery person request {Action} for requestId: {RequestId}", action, requestId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling delivery person request for requestId: {RequestId}", requestId);
                return BadRequest($"Failed to handle request: {ex.Message}");
            }
        }

        [HttpGet("available-delivery-persons")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableDeliveryPersons([FromQuery] string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                _logger.LogWarning("Address is required for fetching available delivery persons.");
                return BadRequest("Address is required.");
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
                        IsAvailable = dp.IsAvailable ?? false,
                        CreatedAt = dp.CreatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} available delivery persons for address: {Address}", deliveryPersons.Count, address);
                return Ok(deliveryPersons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch available delivery persons for address: {Address}", address);
                return BadRequest($"Failed to fetch delivery persons: {ex.Message}");
            }
        }

        [HttpPost("assign-order")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignOrder([FromBody] AssignOrderDto assignOrderDto)
        {
            try
            {
                var order = await _dbContext.Orders.FindAsync(assignOrderDto.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("Order not found for orderId: {OrderId}", assignOrderDto.OrderId);
                    return BadRequest($"Order with ID {assignOrderDto.OrderId} not found.");
                }

                var deliveryPerson = await _dbContext.DeliveryPersons.FirstOrDefaultAsync(dp => dp.Id == assignOrderDto.DeliveryPersonId);
                if (deliveryPerson == null)
                {
                    _logger.LogWarning("Delivery person not found for deliveryPersonId: {DeliveryPersonId}", assignOrderDto.DeliveryPersonId);
                    return BadRequest($"Delivery person with ID {assignOrderDto.DeliveryPersonId} not found.");
                }

                if (deliveryPerson.RequestStatus != "Approved" || deliveryPerson.IsAvailable != true)
                {
                    _logger.LogWarning("Delivery person is not approved or available for deliveryPersonId: {DeliveryPersonId}", assignOrderDto.DeliveryPersonId);
                    return BadRequest("Delivery person is not approved or available.");
                }

                if (string.Compare(deliveryPerson.Address, order.Address, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    _logger.LogWarning("Delivery person address does not match order address for deliveryPersonId: {DeliveryPersonId}, orderId: {OrderId}", assignOrderDto.DeliveryPersonId, assignOrderDto.OrderId);
                    return BadRequest("Delivery person address does not match order address.");
                }

                order.DeliveryPersonId = assignOrderDto.DeliveryPersonId;
                order.Status = OrderStatus.Assigned;
                deliveryPerson.IsAvailable = false;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} assigned to delivery person {DeliveryPersonId}", assignOrderDto.OrderId, assignOrderDto.DeliveryPersonId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign order {OrderId} to delivery person {DeliveryPersonId}", assignOrderDto.OrderId, assignOrderDto.DeliveryPersonId);
                return BadRequest($"Failed to assign order: {ex.Message}");
            }
        }

        [HttpGet("statistics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetOrderStatistics()
        {
            try
            {
                var statistics = _dbContext.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToDictionary(k => k.Status, v => v.Count);

                _logger.LogInformation("Fetched order statistics");
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch order statistics");
                return BadRequest($"Failed to fetch statistics: {ex.Message}");
            }
        }

        [HttpGet("orders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch orders");
                return BadRequest($"Failed to fetch orders: {ex.Message}");
            }
        }
    }
}
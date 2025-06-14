using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GraduationProject.Core.Dtos;
using Microsoft.AspNetCore.Http;
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
                var deliveryPerson = await _dbContext.DeliveryPersons.FindAsync(requestId);
                if (deliveryPerson == null)
                {
                    _logger.LogWarning("Delivery person not found for requestId: {RequestId}", requestId);
                    return BadRequest($"Delivery person with ID {requestId} not found.");
                }

                if (action.ToLower() == "approve")
                {
                    deliveryPerson.RequestStatus = "Approved";
                    deliveryPerson.IsAvailable = true;
                }
                else if (action.ToLower() == "reject")
                {
                    deliveryPerson.RequestStatus = "Rejected";
                    deliveryPerson.IsAvailable = false;
                }
                else
                {
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
    }
}
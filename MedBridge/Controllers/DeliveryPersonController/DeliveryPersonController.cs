using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using GraduationProject.Core.Services;
using GraduationProject.Core.Dtos;
using Microsoft.AspNetCore.Http;
using GraduationProject.Core.Interfaces;
using Microsoft.Extensions.Logging;
using MedBridge.Dtos.DeliveryPersonRequestDto;

namespace GraduationProject.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryPersonController : ControllerBase
    {
        private readonly IDeliveryPersonService _deliveryPersonService;
        private readonly ILogger<DeliveryPersonController> _logger;

        public DeliveryPersonController(IDeliveryPersonService deliveryPersonService, ILogger<DeliveryPersonController> logger)
        {
            _deliveryPersonService = deliveryPersonService ?? throw new ArgumentNullException(nameof(deliveryPersonService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("submit-request")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SubmitDeliveryPersonRequest([FromBody] DeliveryPersonRequestDto requestDto, [FromQuery] int userid)
        {
            if (!ModelState.IsValid || requestDto == null)
            {
                _logger.LogWarning("Invalid request data received for userId: {UserId}", userid);
                return BadRequest("Invalid request data.");
            }

            try
            {
                _logger.LogInformation("Submitting delivery person request for userId: {UserId}", userid);
                var result = await _deliveryPersonService.SubmitDeliveryPersonRequestAsync(requestDto, userid);
                return CreatedAtAction(nameof(GetDeliveryPersonRequests), new { message = result }, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit delivery person request for userId: {UserId}", userid);
                return BadRequest($"Failed to submit request: {ex.Message}");
            }
        }

        [HttpGet("requests")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDeliveryPersonRequests()
        {
            try
            {
                var requests = await _deliveryPersonService.GetAllRequestsAsync();
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
                await _deliveryPersonService.HandleDeliveryPersonRequestAsync(requestId, action);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle delivery person request for requestId: {RequestId}", requestId);
                return BadRequest($"Failed to handle request: {ex.Message}");
            }
        }

        [HttpGet("userId")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDeliveryPersonById([FromQuery] int userId)
        {
            try
            {
                var result = await _deliveryPersonService.GetDeliveryPersonData(userId);

                if (result == null || result.Count == 0)
                {
                    return NotFound($"No delivery person data found for userId: {userId}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get delivery data for userId: {UserId}", userId);
                return BadRequest($"Failed to get delivery data: {ex.Message}");
            }
        }

        [HttpGet("info/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDeliveryInfo(int userId)
        {
            try
            {
                var result = await _deliveryPersonService.GetDeliveryPersonData(userId);

                if (result == null || result.Count == 0)
                {
                    return NotFound($" no delivery person data found for userId: {userId}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get delivery data for userId: {UserId}", userId);
                return BadRequest($"Failed to get delivery data: {ex.Message}");
            }
        }

        [HttpGet("data/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDeliveryPersonDataByUserId(int userId)
        {
            try
            {
                var result = await _deliveryPersonService.GetDeliveryPersonData(userId);

                if (result == null || result.Count == 0)
                {
                    return NotFound($"No delivery person data found for userId: {userId}");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get delivery person data for userId: {UserId}", userId);
                return BadRequest($"Failed to get delivery person data: {ex.Message}");
            }
        }

        [HttpPatch("availability")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAvailability([FromQuery] int userId, [FromBody] UpdateAvailabilityDto availabilityDto)
        {
            try
            {
                await _deliveryPersonService.UpdateAvailabilityAsync(userId, availabilityDto.IsAvailable);
                _logger.LogInformation("Availability updated to {IsAvailable} for userId: {UserId}", availabilityDto.IsAvailable, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update availability for userId: {UserId}", userId);
                return BadRequest($"Failed to update availability: {ex.Message}");
            }
        }
    }

  
}
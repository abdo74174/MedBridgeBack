using MedbridgeApi.Models;
using MedbridgeApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedbridgeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuyerRequestController : ControllerBase
    {
        private readonly IBuyerRequestService _service;

        public BuyerRequestController(IBuyerRequestService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] BuyerRequest request , int userid)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.CreateRequestAsync(request, userid); // Adjust userId as needed
                return CreatedAtAction(nameof(GetRequest), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRequests()
        {
            try
            {
                var requests = await _service.GetAllRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            try
            {
                var request = await _service.GetRequestByIdAsync(id);
                if (request == null)
                    return NotFound($"Request with ID {id} not found");
                return Ok(request);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateRequestStatus(int id, [FromBody] StatusUpdateModel statusUpdate)
        {
            if (statusUpdate == null || string.IsNullOrEmpty(statusUpdate.Status))
                return BadRequest("Status is required");

            if (statusUpdate.Status != "Accepted" && statusUpdate.Status != "Rejected" && statusUpdate.Status != "Pending")
                return BadRequest("Invalid status. Must be 'Accepted', 'Rejected', or 'Pending'");

            try
            {
                var success = await _service.UpdateRequestStatusAsync(id, statusUpdate.Status);
                if (!success)
                    return NotFound($"Request with ID {id} not found");
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var success = await _service.DeleteRequestAsync(id);
                if (!success)
                    return NotFound($"Request with ID {id} not found");
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class StatusUpdateModel
    {
        public string Status { get; set; } = string.Empty;
    }
}
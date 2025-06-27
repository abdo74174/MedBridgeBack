using GraduationProject.Core.Dtos;
using GraduationProject.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GraduationProject.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryPersonAdminController : ControllerBase
    {
        private readonly IDeliveryPersonAdminService _deliveryPersonAdminService;

        public DeliveryPersonAdminController(IDeliveryPersonAdminService deliveryPersonAdminService)
        {
            _deliveryPersonAdminService = deliveryPersonAdminService;
        }

        [HttpGet("requests")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDeliveryPersonRequests()
        {
            return await _deliveryPersonAdminService.GetDeliveryPersonRequests();
        }

        [HttpPut("request/{requestId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> HandleDeliveryPersonRequest(int requestId, [FromQuery] string action)
        {
            return await _deliveryPersonAdminService.HandleDeliveryPersonRequest(requestId, action);
        }

        [HttpGet("available-delivery-persons")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableDeliveryPersons([FromQuery] string address)
        {
            return await _deliveryPersonAdminService.GetAvailableDeliveryPersons(address);
        }

        [HttpPost("assign-order")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignOrder([FromBody] AssignOrderDto assignOrderDto)
        {
            return await _deliveryPersonAdminService.AssignOrder(assignOrderDto);
        }

        [HttpGet("statistics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetOrderStatistics()
        {
            return _deliveryPersonAdminService.GetOrderStatistics();
        }

        [HttpGet("orders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetOrders()
        {
            return _deliveryPersonAdminService.GetOrders();
        }
    }
}
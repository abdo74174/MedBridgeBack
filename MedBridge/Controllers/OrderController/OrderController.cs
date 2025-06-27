using MedBridge.Dtos.DeliveryPersonRequestDto;
using MedBridge.Dtos.OrderDtos;
using MedBridge.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _orderService.CreateOrder(dto);
        }

        [HttpPut("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromQuery] string status)
        {
            return await _orderService.UpdateOrderStatus(id, status);
        }

        [HttpPut("{id}/delivery-status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateOrderStatusByDeliveryPerson(int id, [FromQuery] int deliveryPersonId, [FromQuery] string status)
        {
            return await _orderService.UpdateOrderStatusByDeliveryPerson(id, deliveryPersonId, status);
        }

        [HttpPut("{id}/user-confirm-shipped")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UserConfirmShipped(int id, [FromQuery] int userId)
        {
            return await _orderService.UserConfirmShipped(id, userId);
        }

        [HttpPut("{id}/assign-delivery")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignDeliveryPerson(int id, [FromQuery] int deliveryPersonId)
        {
            return await _orderService.AssignDeliveryPerson(id, deliveryPersonId);
        }

        [HttpGet("delivery/{deliveryPersonId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByDeliveryPerson(int deliveryPersonId)
        {
            return await _orderService.GetOrdersByDeliveryPerson(deliveryPersonId);
        }

        [HttpGet("user/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            return await _orderService.GetOrdersByUser(userId);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetOrders()
        {
            return _orderService.GetOrders();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            return await _orderService.DeleteOrder(id);
        }

        [HttpGet("delivery-persons")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableDeliveryPersons()
        {
            return await _orderService.GetAvailableDeliveryPersons();
        }
    }
}
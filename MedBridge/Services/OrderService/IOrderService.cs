using MedBridge.Dtos.DeliveryPersonRequestDto;
using MedBridge.Dtos.OrderDtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface IOrderService
    {
        Task<IActionResult> CreateOrder(CreateOrderDto dto);
        Task<IActionResult> UpdateOrderStatus(int id, string status);
        Task<IActionResult> UpdateOrderStatusByDeliveryPerson(int id, int deliveryPersonId, string status);
        Task<IActionResult> UserConfirmShipped(int id, int userId);
        Task<IActionResult> AssignDeliveryPerson(int id, int deliveryPersonId);
        Task<IActionResult> GetOrdersByDeliveryPerson(int deliveryPersonId);
        Task<IActionResult> GetOrdersByUser(int userId);
        IActionResult GetOrders();
        Task<IActionResult> DeleteOrder(int id);
        Task<IActionResult> GetAvailableDeliveryPersons();
    }
}
using GraduationProject.Core.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GraduationProject.Core.Services
{
    public interface IDeliveryPersonAdminService
    {
        Task<IActionResult> GetDeliveryPersonRequests();
        Task<IActionResult> HandleDeliveryPersonRequest(int requestId, string action);
        Task<IActionResult> GetAvailableDeliveryPersons(string address);
        Task<IActionResult> AssignOrder(AssignOrderDto assignOrderDto);
        IActionResult GetOrderStatistics();
        IActionResult GetOrders();
    }
}
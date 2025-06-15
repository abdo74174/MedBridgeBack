using GraduationProject.Core.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraduationProject.Core.Interfaces
{
    public interface IDeliveryPersonService
    {
        Task<string> SubmitDeliveryPersonRequestAsync(DeliveryPersonRequestDto requestDto, int userId);
        Task<List<DeliveryPersonRequestDto>> GetAllRequestsAsync();
        Task<List<DeliveryPersonRequestDto>> GetDeliveryPersonData(int userId);
        Task HandleDeliveryPersonRequestAsync(int requestId, string action);
        Task UpdateAvailabilityAsync(int userId, bool isAvailable);
    }
}
using MedbridgeApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedbridgeApi.Services
{
    public interface IBuyerRequestService
    {
        Task<BuyerRequest> CreateRequestAsync(BuyerRequest request, int userId);
        Task<List<BuyerRequest>> GetAllRequestsAsync();
        Task<BuyerRequest> GetRequestByIdAsync(int id);
        Task<bool> UpdateRequestStatusAsync(int id, string status);
        Task<bool> DeleteRequestAsync(int id);
    }

}
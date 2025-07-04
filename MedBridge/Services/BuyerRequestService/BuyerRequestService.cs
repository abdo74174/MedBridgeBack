using MedbridgeApi.Models;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedbridgeApi.Services
{
  

    public class BuyerRequestService : IBuyerRequestService
    {
        private readonly ApplicationDbContext _context;

        public BuyerRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BuyerRequest> CreateRequestAsync(BuyerRequest request, int userId)
        {
            var user = await _context.users.FirstOrDefaultAsync(e => e.Id == userId);
            Console.WriteLine($"🔍 Searching for user with ID = {userId} -> Found = {user != null}");
            Console.WriteLine($"📦 Using DB: {_context.Database.GetDbConnection().Database}");

            if (user == null)
            {
                throw new ArgumentException("User not found.");
            }

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            user.isBuyer = true;
            request.Status = "Pending";
            request.CreatedAt = DateTime.UtcNow;
            _context.BuyerRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<List<BuyerRequest>> GetAllRequestsAsync()
        {
            return await _context.BuyerRequests.ToListAsync();
        }

        public async Task<BuyerRequest> GetRequestByIdAsync(int id)
        {
            return await _context.BuyerRequests.FindAsync(id);
        }

        public async Task<bool> UpdateRequestStatusAsync(int id, string status)
        {
            var request = await _context.BuyerRequests.FindAsync(id);
            if (request == null)
                return false;

            if (status != "Pending" && status != "Accepted" && status != "Rejected")
                throw new ArgumentException("Invalid status");

            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRequestAsync(int id)
        {
            var request = await _context.BuyerRequests.FindAsync(id);
            if (request == null)
                return false;

            _context.BuyerRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
using GraduationProject.Core.Entities;
using GraduationProject.Core.Interfaces;
using GraduationProject.Core.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MoviesApi.models;

namespace GraduationProject.Core.Services
{
    public class DeliveryPersonService : IDeliveryPersonService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DeliveryPersonService> _logger;

        public DeliveryPersonService(ApplicationDbContext dbContext, ILogger<DeliveryPersonService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SubmitDeliveryPersonRequestAsync(DeliveryPersonRequestDto requestDto, int userId)
        {
            try
            {
                var user = await _dbContext.users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for userId: {UserId}", userId);
                    return "User not found.";
                }

                var existingDeliveryPerson = await _dbContext.DeliveryPersons.FirstOrDefaultAsync(dp => dp.Id == userId);
                if (existingDeliveryPerson != null)
                {
                    _logger.LogWarning("Delivery person request already exists for userId: {UserId}", userId);
                    return "Delivery person request already submitted.";
                }

                var deliveryPerson = new DeliveryPerson
                {

                    Name = user.Name,
                    Email = user.Email,
                    Phone = requestDto.Phone,
                    Address = requestDto.Address,
                    CardNumber = requestDto.CardNumber,
                    RequestStatus = "Pending",
                    IsAvailable = false,
                    PasswordHash = user.PasswordHash,
                    PasswordSalt = user.PasswordSalt,
                    CreatedAt = user.CreatedAt,
                    KindOfWork = user.KindOfWork,
                    StripeCustomerId = user.StripeCustomerId,
                    IsAdmin = user.IsAdmin,
                    Status = user.Status,
                    ProfileImage = user.ProfileImage
                };

                _dbContext.Entry(user).State = EntityState.Detached;
                _dbContext.DeliveryPersons.Add(deliveryPerson);

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Delivery person request submitted successfully for userId: {UserId}", userId);

                return $"Delivery person request submitted successfully for user {deliveryPerson.Name}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting delivery person request for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<DeliveryPersonRequestDto>> GetAllRequestsAsync()
        {
            try
            {
                return await _dbContext.DeliveryPersons
                    .Select(dp => new DeliveryPersonRequestDto
                    {
                        Phone = dp.Phone,
                        Address = dp.Address,
                        RequestStatus = dp.RequestStatus,
                        CardNumber = dp.CardNumber,
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all delivery person requests");
                throw;
            }
        }

        public async Task HandleDeliveryPersonRequestAsync(int requestId, string action)
        {
            try
            {
                var deliveryPerson = await _dbContext.DeliveryPersons.FindAsync(requestId);
                if (deliveryPerson == null)
                {
                    _logger.LogWarning("Delivery person not found for requestId: {RequestId}", requestId);
                    throw new Exception($"Delivery person with ID {requestId} not found.");
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
                    throw new Exception($"Invalid action: {action}.");
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Delivery person request {Action} for requestId: {RequestId}", action, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling delivery person request for requestId: {RequestId}", requestId);
                throw;
            }
        }
    }
}
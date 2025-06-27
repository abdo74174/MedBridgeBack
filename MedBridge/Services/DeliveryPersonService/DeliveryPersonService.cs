using GraduationProject.Core.Entities;
using GraduationProject.Core.Interfaces;
using GraduationProject.Core.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedBridge.Models;
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

        public async Task<string> SubmitDeliveryPersonRequestAsync(DeliveryPersonRequestDto requestDto, int? userId = null)
        {
            try
            {
                // Validate input
                if (requestDto == null)
                {
                    _logger.LogWarning("Delivery person request DTO is null.");
                    return "Invalid request data.";
                }

                User user;

                // Check if userId is provided and user exists
                if (userId.HasValue)
                {
                    user = await _dbContext.users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found for userId: {UserId}", userId);
                        return "User not found.";
                    }

                    // Update user details if provided in DTO
                    user.Name = requestDto.Name;
                    user.Email = requestDto.Email;
                    user.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Check if user with the same email already exists
                    user = await _dbContext.users.FirstOrDefaultAsync(u => u.Email == requestDto.Email);
                    if (user != null)
                    {
                        _logger.LogWarning("User with email {Email} already exists.", requestDto.Email);
                        return "A user with this email already exists.";
                    }

                    // Create new user
                    user = new User
                    {
                        Name = requestDto.Name,
                        Email = requestDto.Email,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.users.Add(user);
                    await _dbContext.SaveChangesAsync(); // Save to generate user Id
                }

                // Check for existing delivery person request
                var existingDeliveryPerson = await _dbContext.DeliveryPersons.FirstOrDefaultAsync(dp => dp.UserId == user.Id);
                if (existingDeliveryPerson != null)
                {
                    _logger.LogWarning("Delivery person request already exists for userId: {UserId}", user.Id);
                    return "Delivery person request already submitted.";
                }

                // Create new delivery person request
                var deliveryPerson = new DeliveryPerson
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = requestDto.Phone,
                    Address = requestDto.Address,
                    CardNumber = requestDto.CardNumber,
                    RequestStatus = "Pending",
                    IsAvailable = false,
                    CreatedAt = user.CreatedAt
                };

                _dbContext.DeliveryPersons.Add(deliveryPerson);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Delivery person request submitted successfully for userId: {UserId}", user.Id);
                return $"Delivery person request submitted successfully for user {deliveryPerson.Name}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting delivery person request for email: {Email}", requestDto.Email);
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
                        DeliveryPersonId = dp.Id,
                        Phone = dp.Phone,
                        Address = dp.Address,
                        RequestStatus = dp.RequestStatus,
                        CardNumber = dp.CardNumber,
                        IsAvailable = dp.IsAvailable,
                        UserId = dp.UserId,
                        Name = dp.Name,
                        Email = dp.Email
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all delivery person requests");
                throw;
            }
        }

        public async Task<List<DeliveryPersonRequestDto>> GetDeliveryPersonData(int userId)
        {
            try
            {
                return await _dbContext.DeliveryPersons
                    .Where(dp => dp.UserId == userId)
                    .Select(dp => new DeliveryPersonRequestDto
                    {
                        DeliveryPersonId = dp.Id,
                        Phone = dp.Phone,
                        Address = dp.Address,
                        RequestStatus = dp.RequestStatus,
                        CardNumber = dp.CardNumber,
                        IsAvailable = dp.IsAvailable,
                        UserId = dp.UserId,
                        Name = dp.Name,
                        Email = dp.Email
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching delivery person data for userId: {UserId}", userId);
                throw;
            }
        }

        public async Task HandleDeliveryPersonRequestAsync(int requestId, string action)
        {
            try
            {
                var deliveryPerson = await _dbContext.DeliveryPersons.FirstOrDefaultAsync(dp => dp.Id == requestId);
                if (deliveryPerson == null)
                {
                    _logger.LogWarning("Delivery person not found for requestId: {RequestId}", requestId);
                    throw new Exception($"Delivery person with ID {requestId} not found.");
                }

                switch (action.ToLower())
                {
                    case "approve":
                        deliveryPerson.RequestStatus = "Approved";
                        deliveryPerson.IsAvailable = true;
                        break;
                    case "reject":
                        deliveryPerson.RequestStatus = "Rejected";
                        deliveryPerson.IsAvailable = false;
                        break;
                    case "pending":
                        deliveryPerson.RequestStatus = "Pending";
                        deliveryPerson.IsAvailable = false;
                        break;
                    default:
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

        public async Task UpdateAvailabilityAsync(int userId, bool isAvailable)
        {
            try
            {
                var deliveryPerson = await _dbContext.DeliveryPersons
                    .FirstOrDefaultAsync(dp => dp.UserId == userId && dp.RequestStatus == "Approved");
                if (deliveryPerson == null)
                {
                    _logger.LogWarning("Approved delivery person not found for userId: {UserId}", userId);
                    throw new Exception("Approved delivery person not found.");
                }

                deliveryPerson.IsAvailable = isAvailable;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Availability updated to {IsAvailable} for userId: {UserId}", isAvailable, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating availability for userId: {UserId}", userId);
                throw;
            }
        }
    }
}
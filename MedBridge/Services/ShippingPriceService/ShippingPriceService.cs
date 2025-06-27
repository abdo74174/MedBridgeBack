using MedBridge.Dtos.ShippingPriceDto;
using MedicalStoreAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApi.models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public class ShippingPriceService : IShippingPriceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShippingPriceService> _logger;

        public ShippingPriceService(ApplicationDbContext context, ILogger<ShippingPriceService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> GetShippingPrices()
        {
            try
            {
                var shippingPrices = await _context.ShippingPrices.ToListAsync();
                _logger.LogInformation("Retrieved {Count} shipping prices", shippingPrices.Count);
                return new OkObjectResult(shippingPrices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch shipping prices");
                return new ObjectResult(new { error = $"Failed to fetch shipping prices: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> UpdateShippingPrice(ShippingPriceUpdateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Governorate))
                {
                    _logger.LogWarning("Update shipping price failed: Governorate is required");
                    return new BadRequestObjectResult("Governorate is required.");
                }

                if (dto.Price < 0)
                {
                    _logger.LogWarning("Update shipping price failed: Price cannot be negative");
                    return new BadRequestObjectResult("Price cannot be negative.");
                }

                var existingPrice = await _context.ShippingPrices
                    .FirstOrDefaultAsync(sp => sp.Governorate == dto.Governorate);

                if (existingPrice == null)
                {
                    var newPrice = new ShippingPrice
                    {
                        Governorate = dto.Governorate,
                        Price = dto.Price,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ShippingPrices.Add(newPrice);
                    _logger.LogInformation("Added new shipping price for Governorate: {Governorate}", dto.Governorate);
                }
                else
                {
                    existingPrice.Price = dto.Price;
                    existingPrice.UpdatedAt = DateTime.UtcNow;
                    _context.ShippingPrices.Update(existingPrice);
                    _logger.LogInformation("Updated shipping price for Governorate: {Governorate}", dto.Governorate);
                }

                await _context.SaveChangesAsync();
                return new OkObjectResult(existingPrice ?? new ShippingPrice { Governorate = dto.Governorate, Price = dto.Price });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update shipping price for Governorate: {Governorate}", dto.Governorate);
                return new ObjectResult(new { error = $"Failed to update shipping price: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
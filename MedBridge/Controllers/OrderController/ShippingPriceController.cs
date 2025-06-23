using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalStoreAPI.Models;
using MoviesApi.models;

namespace MedicalStoreAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ShippingPriceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ShippingPriceController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/ShippingPrice
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShippingPrice>>> GetShippingPrices()
    {
        try
        {
            var shippingPrices = await _context.ShippingPrices.ToListAsync();
            return Ok(shippingPrices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to fetch shipping prices: {ex.Message}" });
        }
    }

    // POST: api/ShippingPrice
    [HttpPost]
    public async Task<ActionResult<ShippingPrice>> UpdateShippingPrice([FromBody] ShippingPriceUpdateDto dto)
    {
        try
        {
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
            }
            else
            {
                existingPrice.Price = dto.Price;
                existingPrice.UpdatedAt = DateTime.UtcNow;
                _context.ShippingPrices.Update(existingPrice);
            }

            await _context.SaveChangesAsync();
            return Ok(existingPrice ?? new ShippingPrice { Governorate = dto.Governorate, Price = dto.Price });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Failed to update shipping price: {ex.Message}" });
        }
    }
}

public class ShippingPriceUpdateDto
{
    public string Governorate { get; set; } = string.Empty;
    public double Price { get; set; }
}
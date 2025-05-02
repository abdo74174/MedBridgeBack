using MedBridge.DTOs;
using MedBridge.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [Route("api/favourites")]
    [ApiController]
    public class FavouritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FavouritesController(ApplicationDbContext context)
        {
            _context = context;
        }

     

        [HttpPost("add")]
        public async Task<IActionResult> AddToFavourites([FromBody] AddToFavouritesDto model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserId))
                    return BadRequest("User ID is required.");

                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                    return NotFound("Product not found.");

                var existing = await _context.Favourites
                    .FirstOrDefaultAsync(f => f.UserId == model.UserId && f.ProductId == model.ProductId);

                if (existing != null)
                    return BadRequest("Product already in favourites.");

                var favourite = new Favourite
                {
                    UserId = model.UserId,
                    ProductId = model.ProductId
                };

                _context.Favourites.Add(favourite);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Product added to favourites", Favourite = favourite });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred.", Error = ex.Message });
            }
        }

        public class RemoveFromFavouritesDto
        {
            public string UserId { get; set; }
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromFavourites(int productId, [FromBody] RemoveFromFavouritesDto model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserId))
                    return BadRequest("User ID is required.");

                var favourite = await _context.Favourites
                    .FirstOrDefaultAsync(f => f.UserId == model.UserId && f.ProductId == productId);

                if (favourite == null)
                    return NotFound("Product not in favourites.");

                _context.Favourites.Remove(favourite);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Product removed from favourites" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred.", Error = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUserFavourites([FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("User ID is required.");

                var favourites = await _context.Favourites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Product)
                    .ToListAsync();

                return Ok(favourites);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred.", Error = ex.Message });
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using RatingApi.Models;

namespace RatingApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RatingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RatingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    
    [HttpPost]
    public async Task<ActionResult<Rating>> CreateRating(Rating rating)
    {
        if (rating.RatingValue < 1 || rating.RatingValue > 5)
        {
            return BadRequest("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(rating.ProductId) || string.IsNullOrWhiteSpace(rating.UserId))
        {
            return BadRequest("ProductId and UserId are required.");
        }

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRating), new { id = rating.Id }, rating);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Rating>>> GetRatings([FromQuery] string? productId)
    {
        var query = _context.Ratings.AsQueryable();
        if (!string.IsNullOrEmpty(productId))
        {
            query = query.Where(r => r.ProductId == productId);
        }
        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Rating>> GetRating(int id)
    {
        var rating = await _context.Ratings.FindAsync(id);
        if (rating == null)
        {
            return NotFound();
        }
        return rating;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRating(int id, Rating rating)
    {
        if (id != rating.Id)
        {
            return BadRequest();
        }

        if (rating.RatingValue < 1 || rating.RatingValue > 5)
        {
            return BadRequest("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(rating.ProductId) || string.IsNullOrWhiteSpace(rating.UserId))
        {
            return BadRequest("ProductId and UserId are required.");
        }

        _context.Entry(rating).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RatingExists(id))
            {
                return NotFound();
            }
            throw;
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRating(int id)
    {
        var rating = await _context.Ratings.FindAsync(id);
        if (rating == null)
        {
            return NotFound();
        }

        _context.Ratings.Remove(rating);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool RatingExists(int id)
    {
        return _context.Ratings.Any(e => e.Id == id);
    }
}
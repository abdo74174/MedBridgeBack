using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApi.models;
using RatingApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RatingApi.Services
{
    public class RatingService : IRatingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RatingService> _logger;

        public RatingService(ApplicationDbContext context, ILogger<RatingService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> CreateRatingAsync(Rating rating)
        {
            try
            {
                if (rating.RatingValue < 1 || rating.RatingValue > 5)
                {
                    _logger.LogWarning("Invalid rating value: {RatingValue}", rating.RatingValue);
                    return new BadRequestObjectResult("Rating must be between 1 and 5.");
                }

                if (string.IsNullOrWhiteSpace(rating.ProductId) || string.IsNullOrWhiteSpace(rating.UserId))
                {
                    _logger.LogWarning("ProductId or UserId is missing for rating creation");
                    return new BadRequestObjectResult("ProductId and UserId are required.");
                }

                _context.Ratings.Add(rating);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created rating {RatingId} for ProductId: {ProductId}, UserId: {UserId}", rating.Id, rating.ProductId, rating.UserId);
                return new CreatedAtActionResult("GetRating", "Ratings", new { id = rating.Id }, rating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rating for ProductId: {ProductId}, UserId: {UserId}", rating.ProductId, rating.UserId);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetRatingsAsync(string? productId)
        {
            try
            {
                var query = _context.Ratings.AsQueryable();
                if (!string.IsNullOrEmpty(productId))
                {
                    query = query.Where(r => r.ProductId == productId);
                }

                var ratings = await query.ToListAsync();
                _logger.LogInformation("Retrieved {Count} ratings for ProductId: {ProductId}", ratings.Count, productId ?? "all");
                return new OkObjectResult(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ratings for ProductId: {ProductId}", productId ?? "all");
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetRatingAsync(int id)
        {
            try
            {
                var rating = await _context.Ratings.FindAsync(id);
                if (rating == null)
                {
                    _logger.LogWarning("Rating not found for ID: {Id}", id);
                    return new NotFoundObjectResult(null);
                }

                _logger.LogInformation("Retrieved rating {RatingId}", id);
                return new OkObjectResult(rating);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rating for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UpdateRatingAsync(int id, Rating rating)
        {
            try
            {
                if (id != rating.Id)
                {
                    _logger.LogWarning("Mismatched rating ID: {Id} does not match {RatingId}", id, rating.Id);
                    return new BadRequestObjectResult("Rating ID mismatch.");
                }

                if (rating.RatingValue < 1 || rating.RatingValue > 5)
                {
                    _logger.LogWarning("Invalid rating value: {RatingValue}", rating.RatingValue);
                    return new BadRequestObjectResult("Rating must be between 1 and 5.");
                }

                if (string.IsNullOrWhiteSpace(rating.ProductId) || string.IsNullOrWhiteSpace(rating.UserId))
                {
                    _logger.LogWarning("ProductId or UserId is missing for rating update ID: {Id}", id);
                    return new BadRequestObjectResult("ProductId and UserId are required.");
                }

                _context.Entry(rating).State = EntityState.Modified;
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated rating {RatingId} for ProductId: {ProductId}, UserId: {UserId}", id, rating.ProductId, rating.UserId);
                    return new NoContentResult();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Ratings.AnyAsync(e => e.Id == id))
                    {
                        _logger.LogWarning("Rating not found for update ID: {Id}", id);
                        return new NotFoundObjectResult(null);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteRatingAsync(int id)
        {
            try
            {
                var rating = await _context.Ratings.FindAsync(id);
                if (rating == null)
                {
                    _logger.LogWarning("Rating not found for ID: {Id}", id);
                    return new NotFoundObjectResult(null);
                }

                _context.Ratings.Remove(rating);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted rating {RatingId}", id);
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting rating for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }
    }
}
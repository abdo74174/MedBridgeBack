using Microsoft.AspNetCore.Mvc;
using RatingApi.Models;
using RatingApi.Services;
using System.Threading.Tasks;

namespace RatingApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingService _ratingService;

        public RatingsController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] Rating rating)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _ratingService.CreateRatingAsync(rating);
        }

        [HttpGet]
        public async Task<IActionResult> GetRatings([FromQuery] string? productId)
        {
            return await _ratingService.GetRatingsAsync(productId);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRating(int id)
        {
            return await _ratingService.GetRatingAsync(id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRating(int id, [FromBody] Rating rating)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _ratingService.UpdateRatingAsync(id, rating);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRating(int id)
        {
            return await _ratingService.DeleteRatingAsync(id);
        }
    }
}
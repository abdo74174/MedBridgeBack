using Microsoft.AspNetCore.Mvc;
using RatingApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RatingApi.Services
{
    public interface IRatingService
    {
        Task<IActionResult> CreateRatingAsync(Rating rating);
        Task<IActionResult> GetRatingsAsync(string? productId);
        Task<IActionResult> GetRatingAsync(int id);
        Task<IActionResult> UpdateRatingAsync(int id, Rating rating);
        Task<IActionResult> DeleteRatingAsync(int id);
    }
}
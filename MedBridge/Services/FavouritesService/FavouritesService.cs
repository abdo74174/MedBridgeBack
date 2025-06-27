using MedBridge.DTOs;
using MedBridge.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MedBridge.Controllers.FavouritesController;

namespace MedBridge.Services
{
    public class FavouritesService : IFavouritesService
    {
        private readonly ApplicationDbContext _context;

        public FavouritesService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> AddToFavourites(AddToFavouritesDto model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserId))
                    return new BadRequestObjectResult("User ID is required.");

                var product = await _context.Products.FindAsync(model.ProductId);
                if (product == null)
                    return new NotFoundObjectResult("Product not found.");

                var existing = await _context.Favourites
                    .FirstOrDefaultAsync(f => f.UserId == model.UserId && f.ProductId == model.ProductId);

                if (existing != null)
                    return new BadRequestObjectResult("Product already in favourites.");

                var favourite = new Favourite
                {
                    UserId = model.UserId,
                    ProductId = model.ProductId
                };

                _context.Favourites.Add(favourite);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { Message = "Product added to favourites", Favourite = favourite });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { Message = "An error occurred.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> RemoveFromFavourites(int productId, RemoveFromFavouritesDto model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserId))
                    return new BadRequestObjectResult("User ID is required.");

                var favourite = await _context.Favourites
                    .FirstOrDefaultAsync(f => f.UserId == model.UserId && f.ProductId == productId);

                if (favourite == null)
                    return new NotFoundObjectResult("Product not in favourites.");

                _context.Favourites.Remove(favourite);
                await _context.SaveChangesAsync();

                return new OkObjectResult(new { Message = "Product removed from favourites" });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { Message = "An error occurred.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> RemoveAllFromFavourites(RemoveFromFavouritesDto model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserId))
                    return new BadRequestObjectResult("User ID is required.");

                var favourites = await _context.Favourites
                    .Where(f => f.UserId == model.UserId)
                    .ToListAsync();

                if (favourites.Any())
                {
                    _context.Favourites.RemoveRange(favourites);
                    await _context.SaveChangesAsync();
                }

                return new OkObjectResult(new { Message = "All favourites removed successfully" });
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { Message = "An error occurred.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> GetUserFavourites(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return new BadRequestObjectResult("User ID is required.");

                var favourites = await _context.Favourites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Product)
                    .ToListAsync();

                return new OkObjectResult(favourites);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { Message = "An error occurred.", Error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
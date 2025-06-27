using MedBridge.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static MedBridge.Controllers.FavouritesController;

namespace MedBridge.Services
{
    public interface IFavouritesService
    {
        Task<IActionResult> AddToFavourites(AddToFavouritesDto model);
        Task<IActionResult> RemoveFromFavourites(int productId, RemoveFromFavouritesDto model);
        Task<IActionResult> RemoveAllFromFavourites(RemoveFromFavouritesDto model);
        Task<IActionResult> GetUserFavourites(string userId);
    }
}
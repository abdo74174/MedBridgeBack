using MedBridge.DTOs;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [Route("api/favourites")]
    [ApiController]
    public class FavouritesController : ControllerBase
    {
        private readonly IFavouritesService _favouritesService;

        public FavouritesController(IFavouritesService favouritesService)
        {
            _favouritesService = favouritesService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToFavourites([FromBody] AddToFavouritesDto model)
        {
            return await _favouritesService.AddToFavourites(model);
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromFavourites(int productId, [FromBody] RemoveFromFavouritesDto model)
        {
            return await _favouritesService.RemoveFromFavourites(productId, model);
        }

        [HttpDelete("removeALL")]
        public async Task<IActionResult> RemoveAllFromFavourites([FromBody] RemoveFromFavouritesDto model)
        {
            return await _favouritesService.RemoveAllFromFavourites(model);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUserFavourites([FromQuery] string userId)
        {
            return await _favouritesService.GetUserFavourites(userId);
        }
    }
}
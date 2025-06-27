using MedBridge.Dtos.ShippingPriceDto;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedicalStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingPriceController : ControllerBase
    {
        private readonly IShippingPriceService _shippingPriceService;

        public ShippingPriceController(IShippingPriceService shippingPriceService)
        {
            _shippingPriceService = shippingPriceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetShippingPrices()
        {
            return await _shippingPriceService.GetShippingPrices();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateShippingPrice([FromBody] ShippingPriceUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _shippingPriceService.UpdateShippingPrice(dto);
        }
    }
}
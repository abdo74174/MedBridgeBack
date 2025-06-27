using MedBridge.Dtos.ProductADD;
using MedBridge.Dtos.ProductDto;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] ProductADDDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _productService.CreateAsync(dto);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            return await _productService.GetAllAsync();
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingProductsAsync()
        {
            return await _productService.GetPendingProductsAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            return await _productService.GetByIdAsync(id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromForm] ProductADDDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _productService.UpdateAsync(id, dto);
        }

        [HttpPut("approve/{id}")]
        public async Task<IActionResult> ApproveProductAsync(int id, [FromBody] ProductApprovalDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _productService.ApproveProductAsync(id, dto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            return await _productService.DeleteAsync(id);
        }

        [HttpGet("recommend")]
        public IActionResult GetRecommendations([FromQuery] int productId, [FromQuery] int topN = 3)
        {
            return _productService.GetRecommendations(productId, topN);
        }

        [HttpGet("images/{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            return _productService.GetImage(fileName);
        }
    }
}
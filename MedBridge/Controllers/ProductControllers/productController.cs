using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MedBridge.Dtos;
using MedBridge.Dtos.ProductADD;
using MedBridge.Models;
using MedBridge.Models.ProductModels;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;

namespace MedBridge.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RecommendationService _recommendationService;
    private readonly string _imageUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "images");
    private readonly string _baseUrl = "https://10.0.2.2:7273";
    private readonly List<string> _allowedExtensions = new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg", ".ico", ".heif" };
    private readonly double _maxAllowedImageSize = 10 * 1024 * 1024;

    public ProductController(ApplicationDbContext dbContext, RecommendationService recommendationService)
    {
        _dbContext = dbContext;
        _recommendationService = recommendationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromForm] ProductADDDto dto)
    {
        if (!await _dbContext.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
            return BadRequest("Invalid Category ID.");

        if (!await _dbContext.subCategories.AnyAsync(s => s.SubCategoryId == dto.SubCategoryId && s.CategoryId == dto.CategoryId))
            return BadRequest("Invalid or mismatched SubCategory ID.");

        if (!await _dbContext.users.AnyAsync(u => u.Id == dto.UserId))
            return BadRequest("Invalid User ID.");

        var imageUrls = new List<string>();
        foreach (var image in dto.Images)
        {
            var ext = Path.GetExtension(image.FileName).ToLower();
            if (!_allowedExtensions.Contains(ext))
                return BadRequest("Unsupported image format.");

            if (image.Length > _maxAllowedImageSize)
                return BadRequest("Image size exceeds 10 MB.");

            var fileName = Guid.NewGuid() + ext;
            var savePath = Path.Combine(_imageUploadPath, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            imageUrls.Add($"{_baseUrl}/images/{fileName}");
        }

        var product = new ProductModel
        {
            ProductId = dto.ProductId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            InstallmentAvailable = dto.InstallmentAvailable,
            IsNew = dto.IsNew,
            StockQuantity = dto.StockQuantity,
            Discount = dto.Discount,
            SubCategoryId = dto.SubCategoryId,
            CategoryId = dto.CategoryId,
            UserId = dto.UserId,
            ImageUrls = imageUrls,
            Status = "Pending" // Set initial status to Pending
        };

        try
        {
            await _dbContext.Products.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            return Ok(product);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var products = await _dbContext.Products
            .Where(p => p.Status == "Approved" && p.isdeleted == false)
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingProductsAsync()
    {
        var products = await _dbContext.Products.Where(p => p.Status == "Pending").ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound("Product not found.");

        return Ok(product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromForm] ProductADDDto dto)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound($"Product with ID {id} not found.");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Discount = dto.Discount;
        product.IsNew = dto.IsNew;
        product.InstallmentAvailable = dto.InstallmentAvailable;
        product.CategoryId = dto.CategoryId;
        product.SubCategoryId = dto.SubCategoryId;

        if (dto.Images != null && dto.Images.Any())
        {
            var imageUrls = new List<string>();
            foreach (var image in dto.Images)
            {
                var ext = Path.GetExtension(image.FileName).ToLower();
                if (!_allowedExtensions.Contains(ext))
                    return BadRequest("Unsupported image format.");

                if (image.Length > _maxAllowedImageSize)
                    return BadRequest("Image size exceeds 10 MB.");

                var fileName = Guid.NewGuid() + ext;
                var savePath = Path.Combine(_imageUploadPath, fileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                imageUrls.Add($"{_baseUrl}/images/{fileName}");
            }

            product.ImageUrls = imageUrls;
        }

        await _dbContext.SaveChangesAsync();
        return Ok(product);
    }

    [HttpPut("approve/{id}")]
    public async Task<IActionResult> ApproveProductAsync(int id, [FromBody] ProductApprovalDto dto)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound($"Product with ID {id} not found.");

        if (dto.Status != "Approved" && dto.Status != "Rejected")
            return BadRequest("Invalid status. Must be 'Approved' or 'Rejected'.");

        product.Status = dto.Status;
        await _dbContext.SaveChangesAsync();
        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
            return NotFound($"Product with ID {id} not found.");

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();
        return Ok("Product deleted successfully.");
    }

    [HttpGet("recommend")]
    public IActionResult GetRecommendations([FromQuery] int productId, [FromQuery] int topN = 3)
    {
        try
        {
            var recommendations = _recommendationService.GetSimilarProducts(productId, topN);
            return Ok(new
            {
                status = "success",
                productId,
                recommendations = recommendations.Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Discount,
                    p.InstallmentAvailable,
                    p.ImageUrls
                })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { status = "error", message = ex.Message });
        }
    }
}

public class ProductApprovalDto
{
    public string Status { get; set; }
}
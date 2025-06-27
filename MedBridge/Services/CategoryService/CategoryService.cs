using MedBridge.Models.ProductModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoviesApi.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CategoryService> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IConfiguration _configuration;
        private readonly List<string> _allowedExtensions = new List<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg", ".ico", ".heif"
        };
        private readonly double _maxAllowedImageSize;

        public CategoryService(
            ApplicationDbContext dbContext,
            ILogger<CategoryService> logger,
            ICloudinaryService cloudinaryService,
            IConfiguration configuration)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _maxAllowedImageSize = _configuration.GetValue<double>("ImageSettings:MaxAllowedImageSize", 10 * 1024 * 1024);
        }

        public async Task<IActionResult> CreateAsync(CategoryDto dto)
        {
            try
            {
                if (dto.Image == null)
                {
                    _logger.LogWarning("Image is required for category creation");
                    return new BadRequestObjectResult("Image is required.");
                }

                var ext = Path.GetExtension(dto.Image.FileName).ToLower();
                if (!_allowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("Invalid image extension: {Extension}", ext);
                    return new BadRequestObjectResult("Only the following image formats are allowed: jpg, jpeg, png, gif, bmp, webp, tiff, tif, svg, ico, heif.");
                }

                if (dto.Image.Length > _maxAllowedImageSize)
                {
                    _logger.LogWarning("Image size {Size} exceeds maximum allowed size {MaxSize}", dto.Image.Length, _maxAllowedImageSize);
                    return new BadRequestObjectResult("Max allowed size for image is 10 MB.");
                }

                var imageUrl = await _cloudinaryService.UploadImageAsync(dto.Image, "categories");

                var category = new Category
                {
                    CategoryId = dto.CategoryId,
                    Name = dto.Name,
                    Description = dto.Description,
                    ImageUrl = imageUrl
                };

                _dbContext.Categories.Add(category);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Created category {CategoryId} with name {Name}", category.CategoryId, category.Name);
                return new OkObjectResult(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category with name {Name}", dto.Name);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var categories = await _dbContext.Categories.ToListAsync();
                _logger.LogInformation("Retrieved {Count} categories", categories.Count);
                return new OkObjectResult(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetByIdAsync(int id)
        {
            try
            {
                var category = await _dbContext.Categories.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category not found for ID: {Id}", id);
                    return new NotFoundObjectResult(null);
                }

                _logger.LogInformation("Retrieved category {CategoryId}", id);
                return new OkObjectResult(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UpdateAsync(int id, CategoryDto dto)
        {
            try
            {
                var category = await _dbContext.Categories.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category not found for ID: {Id}", id);
                    return new NotFoundObjectResult($"ID {id} not found");
                }

                if (dto.Image != null)
                {
                    var ext = Path.GetExtension(dto.Image.FileName).ToLower();
                    if (!_allowedExtensions.Contains(ext))
                    {
                        _logger.LogWarning("Invalid image extension: {Extension}", ext);
                        return new BadRequestObjectResult("Only the following image formats are allowed: jpg, jpeg, png, gif, bmp, webp, tiff, tif, svg, ico, heif.");
                    }

                    if (dto.Image.Length > _maxAllowedImageSize)
                    {
                        _logger.LogWarning("Image size {Size} exceeds maximum allowed size {MaxSize}", dto.Image.Length, _maxAllowedImageSize);
                        return new BadRequestObjectResult("Max allowed size for image is 10 MB.");
                    }

                    var imageUrl = await _cloudinaryService.UploadImageAsync(dto.Image, "categories");
                    category.ImageUrl = imageUrl;
                }

                category.Name = dto.Name;
                category.Description = dto.Description;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Updated category {CategoryId} with name {Name}", id, category.Name);
                return new OkObjectResult(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                var category = await _dbContext.Categories.FindAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category not found for ID: {Id}", id);
                    return new NotFoundObjectResult($"ID {id} not found");
                }

                _dbContext.Categories.Remove(category);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Deleted category {CategoryId}", id);
                return new OkObjectResult(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }
    }
}
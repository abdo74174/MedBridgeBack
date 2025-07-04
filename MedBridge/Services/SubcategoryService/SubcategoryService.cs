using MedBridge.Dtos.Product;
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
    public class SubcategoryService : ISubcategoryService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<SubcategoryService> _logger;
        private readonly IConfiguration _configuration;

        public SubcategoryService(
            ApplicationDbContext dbContext,
            ILogger<SubcategoryService> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IActionResult> CreateAsync(subCategoriesDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.ImageUrl))
                {
                    _logger.LogWarning("ImageUrl is required for subcategory creation");
                    return new BadRequestObjectResult("ImageUrl is required.");
                }

                if (!await _dbContext.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
                {
                    _logger.LogWarning("Invalid Category ID: {CategoryId}", dto.CategoryId);
                    return new BadRequestObjectResult("Invalid Category ID.");
                }

                var subcategory = new subCategory
                {
                    CategoryId = dto.CategoryId,
                    Name = dto.Name,
                    Description = dto.Description,
                    ImageUrl = dto.ImageUrl
                };

                _dbContext.subCategories.Add(subcategory);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Created subcategory {SubCategoryId} with name {Name}", subcategory.SubCategoryId, subcategory.Name);
                return new OkObjectResult(subcategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subcategory with name {Name}", dto.Name);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var subCategories = await _dbContext.subCategories.ToListAsync();
                _logger.LogInformation("Retrieved {Count} subcategories", subCategories.Count);
                return new OkObjectResult(subCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcategories");
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetByIdAsync(int id)
        {
            try
            {
                var subCategory = await _dbContext.subCategories.FindAsync(id);
                if (subCategory == null)
                {
                    _logger.LogWarning("Subcategory not found for ID: {Id}", id);
                    return new NotFoundObjectResult(null);
                }

                _logger.LogInformation("Retrieved subcategory {SubCategoryId}", id);
                return new OkObjectResult(subCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcategory for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UpdateAsync(int id, subCategoriesDto dto)
        {
            try
            {
                var subCategory = await _dbContext.subCategories.FindAsync(id);
                if (subCategory == null)
                {
                    _logger.LogWarning("Subcategory not found for ID: {Id}", id);
                    return new NotFoundObjectResult($"ID {id} not found.");
                }

                if (!await _dbContext.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
                {
                    _logger.LogWarning("Invalid Category ID: {CategoryId}", dto.CategoryId);
                    return new BadRequestObjectResult("Invalid Category ID.");
                }

                subCategory.Name = dto.Name;
                subCategory.Description = dto.Description;
                subCategory.CategoryId = dto.CategoryId;
                if (!string.IsNullOrEmpty(dto.ImageUrl))
                {
                    subCategory.ImageUrl = dto.ImageUrl;
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Updated subcategory {SubCategoryId} with name {Name}", id, subCategory.Name);
                return new OkObjectResult(subCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subcategory for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                var subCategory = await _dbContext.subCategories.FindAsync(id);
                if (subCategory == null)
                {
                    _logger.LogWarning("Subcategory not found for ID: {Id}", id);
                    return new NotFoundObjectResult("Subcategory not found.");
                }

                bool hasProducts = await _dbContext.Products.AnyAsync(p => p.SubCategoryId == id);
                if (hasProducts)
                {
                    _logger.LogWarning("Cannot delete subcategory {SubCategoryId} because it is associated with existing products", id);
                    return new BadRequestObjectResult("Cannot delete subcategory because it is associated with existing products.");
                }

                _dbContext.subCategories.Remove(subCategory);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Deleted subcategory {SubCategoryId}", id);
                return new OkObjectResult("Subcategory deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subcategory for ID: {Id}", id);
                return new ObjectResult($"Error: {ex.Message}") { StatusCode = 500 };
            }
        }
    }
}
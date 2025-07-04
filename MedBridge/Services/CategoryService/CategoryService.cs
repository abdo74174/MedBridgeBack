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
        private readonly IConfiguration _configuration;

        public CategoryService(
            ApplicationDbContext dbContext,
            ILogger<CategoryService> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<IActionResult> CreateAsync(CategoryDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.ImageUrl))
                {
                    _logger.LogWarning("ImageUrl is required for category creation");
                    return new BadRequestObjectResult("ImageUrl is required.");
                }

                var category = new Category
                {
                    CategoryId = dto.CategoryId,
                    Name = dto.Name,
                    Description = dto.Description,
                    ImageUrl = dto.ImageUrl
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

                category.Name = dto.Name;
                category.Description = dto.Description;
                if (!string.IsNullOrEmpty(dto.ImageUrl))
                {
                    category.ImageUrl = dto.ImageUrl;
                }

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
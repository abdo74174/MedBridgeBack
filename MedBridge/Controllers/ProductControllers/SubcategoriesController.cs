using MedBridge.Dtos.Product;
using MedBridge.Models.ProductModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Controllers.ProductControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubcategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly List<string> _allowedExtensions = new List<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg", ".ico", ".heif"
        };

        private readonly double _maxAllowedImageSize = 10 * 1024 * 1024;
        private readonly string _imageUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "images");
        private readonly string _baseUrl = "https://10.0.2.2:7273";

        public SubcategoriesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] subCategoriesDto dto)
        {
           
            if (dto.Image == null)
                return BadRequest("Image is required.");

            var ext = Path.GetExtension(dto.Image.FileName).ToLower();

           
            if (!_allowedExtensions.Contains(ext))
                return BadRequest("Only png and jpg images are allowed.");

            if (dto.Image.Length > _maxAllowedImageSize)
                return BadRequest("Max allowed size for image is 10 MB."); 


        
            var fileName = Guid.NewGuid() + ext;
            var savePath = Path.Combine(_imageUploadPath, fileName);

          
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }

          
            var imageUrl = $"{_baseUrl}/images/{fileName}";

          
            bool validateCategoryID = await _dbContext.Categories.AnyAsync(g => g.CategoryId == dto.CategoryId);
            if (!validateCategoryID)
                return BadRequest("Invalid Category ID.");

            var subcategory = new subCategory
            {
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = imageUrl 
            };

            _dbContext.subCategories.Add(subcategory);
            await _dbContext.SaveChangesAsync();

            return Ok(subcategory);
        }


        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var subCategories = await _dbContext.subCategories.ToListAsync();
            return Ok(subCategories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var subCategory = await _dbContext.subCategories.FindAsync(id);
            if (subCategory == null)
                return NotFound();
            return Ok(subCategory);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromForm] subCategoriesDto dto)
        {
            var subCategory = await _dbContext.subCategories.FindAsync(id);
            if (subCategory == null)
                return NotFound($"ID {id} not found.");

          
            if (dto.Image != null)
            {
                var ext = Path.GetExtension(dto.Image.FileName).ToLower();

                if (!_allowedExtensions.Contains(ext))
                    return BadRequest("Only PNG and JPG images are allowed.");

                if (dto.Image.Length > _maxAllowedImageSize)
                    return BadRequest("Max allowed size for image is 1 MB.");

                var fileName = Guid.NewGuid() + ext;
                var savePath = Path.Combine(_imageUploadPath, fileName);

                if (!Directory.Exists(_imageUploadPath))
                    Directory.CreateDirectory(_imageUploadPath);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(stream);
                }

                var imageUrl = $"{_baseUrl}/images/{fileName}";
                subCategory.ImageUrl = imageUrl;
            }

           
            subCategory.Name = dto.Name;
            subCategory.Description = dto.Description;

           
            bool validateCategoryID = await _dbContext.Categories.AnyAsync(g => g.CategoryId == dto.CategoryId);
            if (!validateCategoryID)
                return BadRequest("Invalid Category ID.");

            subCategory.CategoryId = dto.CategoryId;
            await _dbContext.SaveChangesAsync();

            return Ok(subCategory);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var subCategory = await _dbContext.subCategories.FindAsync(id);

            if (subCategory == null)
                return NotFound("Subcategory not found.");

            bool hasProducts = await _dbContext.Products.AnyAsync(p => p.SubCategoryId == id);
            if (hasProducts)
                return BadRequest("Cannot delete subcategory because it is associated with existing products.");

            _dbContext.subCategories.Remove(subCategory);
            await _dbContext.SaveChangesAsync();

            return Ok("Subcategory deleted successfully.");
        }

    }
}

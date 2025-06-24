using MedBridge.Models.ProductModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System.Linq;

namespace MedBridge.Controllers.ProductControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly List<string> _allowedExtensions = new List<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg", ".ico", ".heif"
        };

        private readonly double _maxAllowedImageSize = 10 * 1024 * 1024;
        private readonly string _imageUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "images");
        private readonly string _baseUrl = "https://10.0.2.2:7273"; 

        public CategoriesController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        [HttpPost]
        public async Task<IActionResult> CreateAsync(CategoryDto dto)
        {
            if (dto.Image == null)
                return BadRequest("Image is required.");

            var ext = Path.GetExtension(dto.Image.FileName).ToLower();
            if (!_allowedExtensions.Contains(ext))
                return BadRequest("Only png and jpg images are allowed.");

            if (dto.Image.Length > _maxAllowedImageSize)
                return BadRequest("Max allowed size for image is 10 MB.");

            // Generate unique file name and save the image
            var fileName = Guid.NewGuid() + ext;
            var savePath = Path.Combine(_imageUploadPath, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }

            var imageUrl = $"{_baseUrl}/images/{fileName}";

            var category = new Category
            {
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = imageUrl // Store only the URL
            };

            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();

            return Ok(category);
        }



        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var categories = await _dbContext.Categories
         
                .ToListAsync();

            return Ok(categories);
        }


        [HttpGet("{id}")]

        public async Task<IActionResult> GetByIdLAsync(int id)
        {

            var category = await _dbContext.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            return Ok(category);

        }


        [HttpPut("{id}")]
        public async Task<IActionResult> updateAsync(int id, [FromForm] CategoryDto dto)
        {
            var category = await _dbContext.Categories.FindAsync(id);

            if (category == null)
                return NotFound($"ID {id} not found");

            if (dto.Image != null)
            {
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
                category.ImageUrl = imageUrl;
            }

            category.Name = dto.Name;
            category.Description = dto.Description;
            await _dbContext.SaveChangesAsync();

            return Ok(category);
        }


        [HttpDelete("{id}")]


        public async Task <IActionResult> deleteAsync(int id)
        {

            var category = await _dbContext.Categories.FindAsync(id);

            if (category == null)
                return NotFound($"ID {id} not found");


            _dbContext.Remove(category);
            _dbContext.SaveChanges();

            return Ok(category);
        }
    }
}

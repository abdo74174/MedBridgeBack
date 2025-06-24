using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MedBridge.Dtos;
using MedBridge.Dtos.ProductADD;
using MedBridge.Models;
using MedBridge.Models.ProductModels;
using MedBridge.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using MoviesApi.models;
using MedBridge.Dtos.ProductDto;

namespace MedBridge.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RecommendationService _recommendationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _imageUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "images");
    private readonly string _baseUrl = "https://10.0.2.2:7273";
    private readonly string _projectId = "medbridge-11d7e";
    private readonly string _serviceAccountPath = "F:\\projects\\Project\\MedBridge\\MedBridge\\wwwroot\\jsonfile\\medbridge-11d7e-firebase-adminsdk-fbsvc-77f183ab5d.json";

    private readonly List<string> _allowedExtensions = new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg" };
    private readonly double _maxAllowedImageSize = 10 * 1024 * 1024;

    public ProductController(ApplicationDbContext dbContext, RecommendationService recommendationService, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _recommendationService = recommendationService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromForm] ProductADDDto dto)
    {
        Console.WriteLine($"Creating product with UserId: {dto.UserId}, CategoryId: {dto.CategoryId}, SubCategoryId: {dto.SubCategoryId}");
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

            Directory.CreateDirectory(Path.GetDirectoryName(savePath)); 
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
            Guarantee = dto.Guarantee,
            StockQuantity = dto.StockQuantity,
            Discount = dto.Discount,
            Address = dto.Address,
            Donation = dto.Donation,
            SubCategoryId = dto.SubCategoryId,
            CategoryId = dto.CategoryId,
            UserId = dto.UserId,
            ImageUrls = imageUrls,
            Status = "Pending"
        };

        try
        {
            await _dbContext.Products.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"Product {product.ProductId} created successfully");
            return Ok(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating product: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var products = await _dbContext.Products
            .Where(p => p.Status == "Approved" && p.isdeleted == false && p.StockQuantity > 0)
            .ToListAsync();
        Console.WriteLine($"Retrieved {products.Count} approved products");
        return Ok(products);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingProductsAsync()
    {
        var products = await _dbContext.Products.Where(p => p.Status == "Pending").ToListAsync();
        Console.WriteLine($"Retrieved {products.Count} pending products");
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == id && p.isdeleted == false);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found.");
        }
        return Ok(product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromForm] ProductADDDto dto)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            Console.WriteLine($"Product {id} not found for update");
            return NotFound($"Product with ID {id} not found.");
        }

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Discount = dto.Discount;
        product.IsNew = dto.IsNew;
        product.Address = dto.Address;
        product.Donation = dto.Donation;
        product.InstallmentAvailable = dto.InstallmentAvailable;
        product.CategoryId = dto.CategoryId;
        product.SubCategoryId = dto.SubCategoryId;
        product.Guarantee = dto.Guarantee;

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

                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                imageUrls.Add($"{_baseUrl}/images/{fileName}");
            }

            product.ImageUrls = imageUrls;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"Product {id} updated successfully");
            return Ok(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating product {id}: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    [HttpPut("approve/{id}")]
    public async Task<IActionResult> ApproveProductAsync(int id, [FromBody] ProductApprovalDto dto)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            Console.WriteLine($"Product {id} not found for approval");
            return NotFound($"Product with ID {id} not found.");
        }

        if (dto.Status != "Approved" && dto.Status != "Rejected")
        {
            Console.WriteLine($"Invalid status for product {id}: {dto.Status}");
            return BadRequest("Invalid status. Must be 'Approved' or 'Rejected'.");
        }

        product.Status = dto.Status;
        await _dbContext.SaveChangesAsync();
        Console.WriteLine($"Product {id} updated to status: {dto.Status}, Discount: {product.Discount}%, UserId: {product.UserId}");

        if (dto.Status == "Approved")
        {
            Console.WriteLine($"Attempting to send discount notification for product {id}");
            await SendDiscountNotification(product);

            if (product.Discount > 30)
            {
                Console.WriteLine($"Attempting to send high-discount notification for product {id} with {product.Discount}% off");
                await SendHighDiscountNotification(product);
            }
        }

        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null)
        {
            Console.WriteLine($"Product {id} not found for deletion");
            return NotFound($"Product with ID {id} not found.");
        }

        product.isdeleted = true;
        await _dbContext.SaveChangesAsync();
        Console.WriteLine($"Product {id} soft-deleted successfully");
        return Ok("Product deleted successfully.");
    }

    [HttpGet("recommend")]
    public IActionResult GetRecommendations([FromQuery] int productId, [FromQuery] int topN = 3)
    {
        try
        {
            var recommendations = _recommendationService.GetSimilarProducts(productId, topN);
            Console.WriteLine($"Retrieved {recommendations.Count} recommendations for product {productId}");
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
                    p.Guarantee,
                    p.InstallmentAvailable,
                    p.ImageUrls
                })
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting recommendations for product {productId}: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { status = "error", message = ex.Message });
        }
    }

    [HttpGet("images/{fileName}")]
    public IActionResult GetImage(string fileName)
    {
        try
        {
            // Sanitize fileName to prevent path traversal
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                Console.WriteLine($"Invalid image file name: {fileName}");
                return BadRequest("Invalid file name.");
            }

            var path = Path.Combine(_imageUploadPath, fileName);
            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine($"Image not found: {path}");
                return NotFound("Image not found.");
            }

            var bytes = System.IO.File.ReadAllBytes(path);
            var contentType = GetContentType(Path.GetExtension(fileName).ToLower());
            Console.WriteLine($"Serving image: {fileName}, Content-Type: {contentType}");
            return File(bytes, contentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error serving image {fileName}: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "Error retrieving image.");
        }
    }

    private string GetContentType(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".tiff" or ".tif" => "image/tiff",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".heif" => "image/heif",
            _ => "application/octet-stream"
        };
    }

    private async Task SendDiscountNotification(ProductModel product)
    {
        try
        {
            //if (!File.Exists(_serviceAccountPath))
            //{
            //    Console.WriteLine($"Service account file not found at: {_serviceAccountPath}");
            //    return;
            //}
            //Console.WriteLine($"Service account file found at: {_serviceAccountPath}");

            var tokens = await _dbContext.DeviceTokens
                .Where(t => t.UserId == product.UserId)
                .ToListAsync();
            Console.WriteLine($"Found {tokens.Count} device tokens for userId: {product.UserId}");

            if (!tokens.Any())
            {
                Console.WriteLine($"No device tokens found for userId: {product.UserId}, skipping notification");
                return;
            }

            var credential = GoogleCredential.FromFile(_serviceAccountPath)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            Console.WriteLine("FCM access token obtained successfully");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            foreach (var token in tokens)
            {
                var message = new
                {
                    message = new
                    {
                        token = token.Token,
                        notification = new
                        {
                            title = "Product Approved!",
                            body = $"Your product {product.Name} has been approved with {product.Discount}% off!"
                        },
                        data = new
                        {
                            click_action = "FLUTTER_NOTIFICATION_CLICK",
                            product_id = product.ProductId.ToString()
                        }
                    }
                };

                var jsonMessage = JsonConvert.SerializeObject(message);
                Console.WriteLine($"Sending FCM message to token {token.Token.Substring(0, 10)}...: {jsonMessage}");

                var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(
                    $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send",
                    content);

                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"FCM response: Status={response.StatusCode}, Body={responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to send notification to token {token.Token.Substring(0, 10)}...: {responseBody}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending notification for product {product.ProductId}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async Task SendHighDiscountNotification(ProductModel product)
    {
        try
        {
            //if (!File.Exists(_serviceAccountPath))
            //{
            //    Console.WriteLine($"Service account file not found at: {_serviceAccountPath}");
            //    return;
            //}
            //Console.WriteLine($"Service account file found at: {_serviceAccountPath}");

            var tokens = await _dbContext.DeviceTokens.ToListAsync();
            Console.WriteLine($"Found {tokens.Count} device tokens for high-discount notification");

            if (!tokens.Any())
            {
                Console.WriteLine("No device tokens found for high-discount notification, skipping");
                return;
            }

            var credential = GoogleCredential.FromFile(_serviceAccountPath)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            Console.WriteLine("FCM access token obtained successfully for high-discount notification");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            foreach (var token in tokens)
            {
                var message = new
                {
                    message = new
                    {
                        token = token.Token,
                        notification = new
                        {
                            title = "Hot Deal!",
                            body = $"{product.Name} is now available with {product.Discount}% off!"
                        },
                        data = new
                        {
                            click_action = "FLUTTER_NOTIFICATION_CLICK",
                            product_id = product.ProductId.ToString()
                        }
                    }
                };

                var jsonMessage = JsonConvert.SerializeObject(message);
                Console.WriteLine($"Sending high-discount FCM message to token {token.Token.Substring(0, 10)}...: {jsonMessage}");

                var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(
                    $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send",
                    content);

                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"FCM response: Status={response.StatusCode}, Body={responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to send high-discount notification to token {token.Token.Substring(0, 10)}...: {responseBody}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending high-discount notification for product {product.ProductId}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}


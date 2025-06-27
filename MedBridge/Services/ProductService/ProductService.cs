using MedBridge.Dtos.ProductADD;
using MedBridge.Dtos.ProductDto;
using MedBridge.Models.NotificationModel;
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
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly RecommendationService _recommendationService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ProductService> _logger;
        private readonly List<string> _allowedExtensions = new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg" };
        private readonly double _maxAllowedImageSize;

        public ProductService(
            ApplicationDbContext dbContext,
            INotificationService notificationService,
            RecommendationService recommendationService,
            ICloudinaryService cloudinaryService,
            IConfiguration configuration,
            ILogger<ProductService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxAllowedImageSize = configuration.GetValue<double>("ImageSettings:MaxAllowedImageSize", 10 * 1024 * 1024);
        }

        public async Task<IActionResult> CreateAsync(ProductADDDto dto)
        {
            try
            {
                _logger.LogInformation("Creating product with UserId: {UserId}, CategoryId: {CategoryId}, SubCategoryId: {SubCategoryId}",
                    dto.UserId, dto.CategoryId, dto.SubCategoryId);

                if (!await _dbContext.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
                {
                    _logger.LogWarning("Invalid Category ID: {CategoryId}", dto.CategoryId);
                    return new BadRequestObjectResult("Invalid Category ID.");
                }

                if (!await _dbContext.subCategories.AnyAsync(s => s.SubCategoryId == dto.SubCategoryId && s.CategoryId == dto.CategoryId))
                {
                    _logger.LogWarning("Invalid or mismatched SubCategory ID: {SubCategoryId}", dto.SubCategoryId);
                    return new BadRequestObjectResult("Invalid or mismatched SubCategory ID.");
                }

                if (!await _dbContext.users.AnyAsync(u => u.Id == dto.UserId))
                {
                    _logger.LogWarning("Invalid User ID: {UserId}", dto.UserId);
                    return new BadRequestObjectResult("Invalid User ID.");
                }

                if (dto.Images == null || !dto.Images.Any())
                {
                    _logger.LogWarning("At least one image is required for product creation");
                    return new BadRequestObjectResult("At least one image is required.");
                }

                var imageUrls = new List<string>();
                foreach (var image in dto.Images)
                {
                    var ext = Path.GetExtension(image.FileName).ToLower();
                    if (!_allowedExtensions.Contains(ext))
                    {
                        _logger.LogWarning("Unsupported image format: {Extension}", ext);
                        return new BadRequestObjectResult("Unsupported image format.");
                    }

                    if (image.Length > _maxAllowedImageSize)
                    {
                        _logger.LogWarning("Image size {Size} exceeds maximum allowed size {MaxSize}", image.Length, _maxAllowedImageSize);
                        return new BadRequestObjectResult("Image size exceeds 10 MB.");
                    }

                    var imageUrl = await _cloudinaryService.UploadImageAsync(image, "products");
                    imageUrls.Add(imageUrl);
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

                await _dbContext.Products.AddAsync(product);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Product {ProductId} created successfully", product.ProductId);
                return new OkObjectResult(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product with UserId: {UserId}", dto.UserId);
                return new ObjectResult(ex.InnerException?.Message ?? ex.Message) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                var products = await _dbContext.Products
                    .Where(p => p.Status == "Approved" && p.isdeleted == false && p.StockQuantity > 0)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} approved products", products.Count);
                return new OkObjectResult(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving approved products");
                return new ObjectResult(ex.Message) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetPendingProductsAsync()
        {
            try
            {
                var products = await _dbContext.Products
                    .Where(p => p.Status == "Pending")
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} pending products", products.Count);
                return new OkObjectResult(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending products");
                return new ObjectResult(ex.Message) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetByIdAsync(int id)
        {
            try
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.ProductId == id && p.isdeleted == false);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for ID: {Id}", id);
                    return new NotFoundObjectResult($"Product with ID {id} not found.");
                }
                _logger.LogInformation("Retrieved product {ProductId}", id);
                return new OkObjectResult(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product for ID: {Id}", id);
                return new ObjectResult(ex.Message) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> UpdateAsync(int id, ProductADDDto dto)
        {
            try
            {
                var product = await _dbContext.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for ID: {Id}", id);
                    return new NotFoundObjectResult($"Product with ID {id} not found.");
                }

                if (!await _dbContext.Categories.AnyAsync(c => c.CategoryId == dto.CategoryId))
                {
                    _logger.LogWarning("Invalid Category ID: {CategoryId}", dto.CategoryId);
                    return new BadRequestObjectResult("Invalid Category ID.");
                }

                if (!await _dbContext.subCategories.AnyAsync(s => s.SubCategoryId == dto.SubCategoryId && s.CategoryId == dto.CategoryId))
                {
                    _logger.LogWarning("Invalid or mismatched SubCategory ID: {SubCategoryId}", dto.SubCategoryId);
                    return new BadRequestObjectResult("Invalid or mismatched SubCategory ID.");
                }

                if (!await _dbContext.users.AnyAsync(u => u.Id == dto.UserId))
                {
                    _logger.LogWarning("Invalid User ID: {UserId}", dto.UserId);
                    return new BadRequestObjectResult("Invalid User ID.");
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
                        {
                            _logger.LogWarning("Unsupported image format: {Extension}", ext);
                            return new BadRequestObjectResult("Unsupported image format.");
                        }

                        if (image.Length > _maxAllowedImageSize)
                        {
                            _logger.LogWarning("Image size {Size} exceeds maximum allowed size {MaxSize}", image.Length, _maxAllowedImageSize);
                            return new BadRequestObjectResult("Image size exceeds 10 MB.");
                        }

                        var imageUrl = await _cloudinaryService.UploadImageAsync(image, "products");
                        imageUrls.Add(imageUrl);
                    }

                    product.ImageUrls = imageUrls;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} updated successfully", id);
                return new OkObjectResult(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product for ID: {Id}", id);
                return new ObjectResult(ex.InnerException?.Message ?? ex.Message) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> ApproveProductAsync(int id, ProductApprovalDto dto)
        {
            try
            {
                var product = await _dbContext.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for ID: {Id}", id);
                    return new NotFoundObjectResult($"Product with ID {id} not found.");
                }

                if (dto.Status != "Approved" && dto.Status != "Rejected")
                {
                    _logger.LogWarning("Invalid status for product {Id}: {Status}", id, dto.Status);
                    return new BadRequestObjectResult("Invalid status. Must be 'Approved' or 'Rejected'.");
                }

                product.Status = dto.Status;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} updated to status: {Status}, Discount: {Discount}%, UserId: {UserId}",
                    id, dto.Status, product.Discount, product.UserId);

                if (dto.Status == "Approved")
                {
                    _logger.LogInformation("Attempting to send discount notification for product {ProductId}", id);
                    await SendDiscountNotification(product);

                    if (product.Discount > 30)
                    {
                        _logger.LogInformation("Attempting to send high-discount notification for product {ProductId} with {Discount}% off", id, product.Discount);
                        await SendHighDiscountNotification(product);
                    }
                }

                return new OkObjectResult(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving product for ID: {Id}", id);
                return new ObjectResult(ex.InnerException?.Message ?? ex.Message) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                var product = await _dbContext.Products.FindAsync(id);
                if (product == null)
                {
                    _logger.LogWarning("Product not found for ID: {Id}", id);
                    return new NotFoundObjectResult($"Product with ID {id} not found.");
                }

                product.isdeleted = true;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Product {ProductId} soft-deleted successfully", id);
                return new OkObjectResult("Product deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product for ID: {Id}", id);
                return new ObjectResult(ex.Message) { StatusCode = 500 };
            }
        }

        public IActionResult GetRecommendations(int productId, int topN)
        {
            try
            {
                var recommendations = _recommendationService.GetSimilarProducts(productId, topN);
                _logger.LogInformation("Retrieved {Count} recommendations for product {ProductId}", recommendations.Count, productId);
                return new OkObjectResult(new
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
                _logger.LogError(ex, "Error getting recommendations for product {ProductId}", productId);
                return new BadRequestObjectResult(new { status = "error", message = ex.Message });
            }
        }

        public IActionResult GetImage(string fileName)
        {
            try
            {
                // Since images are now stored in Cloudinary, redirect to the Cloudinary URL
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    _logger.LogWarning("Invalid image file name: {FileName}", fileName);
                    return new BadRequestObjectResult("Invalid file name.");
                }

                // Assuming fileName is the full Cloudinary URL stored in the database
                return new RedirectResult(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving image {FileName}", fileName);
                return new ObjectResult("Error retrieving image.") { StatusCode = 500 };
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
                var tokens = await _dbContext.DeviceTokens
                    .Where(t => t.UserId == product.UserId)
                    .ToListAsync();
                _logger.LogInformation("Found {Count} device tokens for userId: {UserId}", tokens.Count, product.UserId);

                if (!tokens.Any())
                {
                    _logger.LogWarning("No device tokens found for userId: {UserId}, skipping notification", product.UserId);
                    return;
                }

                foreach (var token in tokens)
                {
                    var notificationRequest = new NotificationRequest
                    {
                        DeviceToken = token.Token,
                        Title = "Product Approved!",
                        Body = $"Your product {product.Name} has been approved with {product.Discount}% off!"
                    };

                    var result = await _notificationService.SendPushNotification(notificationRequest);
                    if (result is not OkObjectResult)
                    {
                        _logger.LogWarning("Failed to send notification to token {Token}: {Result}",
                            token.Token.Substring(0, Math.Min(10, token.Token.Length)) + "...", result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification for product {ProductId}", product.ProductId);
            }
        }

        private async Task SendHighDiscountNotification(ProductModel product)
        {
            try
            {
                var tokens = await _dbContext.DeviceTokens.ToListAsync();
                _logger.LogInformation("Found {Count} device tokens for high-discount notification", tokens.Count);

                if (!tokens.Any())
                {
                    _logger.LogWarning("No device tokens found for high-discount notification, skipping");
                    return;
                }

                foreach (var token in tokens)
                {
                    var notificationRequest = new NotificationRequest
                    {
                        DeviceToken = token.Token,
                        Title = "Hot Deal!",
                        Body = $"{product.Name} is now available with {product.Discount}% off!"
                    };

                    var result = await _notificationService.SendPushNotification(notificationRequest);
                    if (result is not OkObjectResult)
                    {
                        _logger.LogWarning("Failed to send high-discount notification to token {Token}: {Result}",
                            token.Token.Substring(0, Math.Min(10, token.Token.Length)) + "...", result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending high-discount notification for product {ProductId}", product.ProductId);
            }
        }
    }
}
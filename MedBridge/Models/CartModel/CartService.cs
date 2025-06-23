

using MedBridge.Dtos.Product;
using MedBridge.Models;

public class CartService
{
    public CartDto SerializeCart(CartModel cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            // Safely parse UserId to handle any possible errors
            UserId = int.TryParse(cart.UserId, out var uid) ? uid : 0,
            CartItems = cart.CartItems.Select(item => new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Product = item.Product != null ? new ProductDto
                {
                    // Safely map product details with fallback values for possible null fields
                    UserId = item.Product.UserId,
                    ProductId = item.Product.ProductId,
                    Name = item.Product.Name ?? "No name",  // Default if name is null
                    Description = item.Product.Description ?? "No description",  // Default if description is null
                    Price = item.Product.Price,
                    StockQuantity = item.Product.StockQuantity,
                    IsNew = item.Product.IsNew,
                    Discount = item.Product.Discount,
                    CategoryId = item.Product.CategoryId,
                    SubCategoryId = item.Product.SubCategoryId,
                    // Filter out null or empty image URLs
                    Images = item.Product.ImageUrls?.Where(url => !string.IsNullOrEmpty(url)).ToList() ?? new List<string>()
                } : null
            }).ToList()
        };
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetDashboardSummary()
        {
            var productCount = await _context.Products.CountAsync();
            var productReviewCount = await _context.Products.CountAsync(p => p.Status == "Pending");
            var userCount = await _context.users.CountAsync();
            var orderCount = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalPrice);
            var usedproductCount = await _context.Products.Where(p=> p.IsNew == false).CountAsync();
            var newproductCount = await _context.Products.Where(p => p.IsNew).CountAsync();

            var latestProducts = await _context.Products
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.Price,
                    p.CreatedAt
                })
                .ToListAsync();

            return new OkObjectResult(new
            {
                ProductCount = productCount,
                UserCount = userCount,
                OrderCount = orderCount,
                TotalRevenue = totalRevenue,
                LatestProducts = latestProducts,
                ProductReviewCount = productReviewCount,
                UsedproductCount = usedproductCount ,
                NewproductCount = newproductCount
            });
        }
    }
}
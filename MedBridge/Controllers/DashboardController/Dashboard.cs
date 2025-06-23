using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.models;

namespace MedBridge.Controllers.Dashboard
{
    [Route("api/[controller]")]
    [ApiController]
    public class Dashboard : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public Dashboard(ApplicationDbContext context)
        {
            _context = context;
        }


        [HttpGet("summary")]
        public IActionResult GetDashboardSummary()
        {

            var productCount = _context.Products.Count();

            var productReviewCount = _context.Products.Where(p => p.Status == "Pending").Count();

            var userCount = _context.users.Count();


            var orderCount = _context.Orders.Count();


            var totalRevenue = _context.Orders.Sum(o => o.TotalPrice);


            var latestProducts = _context.Products
                                    .OrderByDescending(p => p.CreatedAt)
                                    .Take(5)
                                    .Select(p => new
                                    {
                                        p.ProductId,
                                        p.Name,
                                        p.Price,
                                        p.CreatedAt
                                    })
                                    .ToList();
            return Ok(new
            {
                ProductCount = productCount,
                UserCount = userCount,
                OrderCount = orderCount,
                TotalRevenue = totalRevenue,
                LatestProducts = latestProducts,
                ProductReviewCount = productReviewCount
            });


        }
    }
}
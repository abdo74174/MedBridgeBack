//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using MedBridge.Models.OrderModels;
//using MoviesApi.models;

//namespace CouponApi.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class CouponController : ControllerBase
//    {
//        private readonly ApplicationDbContext _context;

//        public CouponController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetCoupons()
//        {
//            var coupons = await _context.Coupons.Include(c => c.Orders).ToListAsync();
//            return Ok(coupons);
//        }

//        [HttpGet("{id}")]
//        public async Task<IActionResult> GetCoupon(int id)
//        {
//            var coupon = await _context.Coupons.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
//            if (coupon == null) return NotFound();
//            return Ok(coupon);
//        }

//        [HttpGet("user/{userId}")]
//        public async Task<IActionResult> GetUserCoupons(int userId)
//        {
//            var userCoupons = await _context.Coupons
//                .Where(c => c.UserId == userId && !c.IsUsed)
//                .ToListAsync();
//            return Ok(userCoupons);
//        }

//        [HttpPost]
//        public async Task<IActionResult> CreateCoupon([FromBody] Coupon coupon)
//        {
//            if (!ModelState.IsValid) return BadRequest(ModelState);

//            _context.Coupons.Add(coupon);
//            await _context.SaveChangesAsync();
//            return CreatedAtAction(nameof(GetCoupon), new { id = coupon.Id }, coupon);
//        }

//        [HttpPut("{id}")]
//        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] Coupon updated)
//        {
//            var existing = await _context.Coupons.FindAsync(id);
//            if (existing == null) return NotFound();

//            existing.Code = updated.Code;
//            existing.DiscountPercentage = updated.DiscountPercentage;
//            existing.UserId = updated.UserId;
//            existing.IsUsed = updated.IsUsed;

//            await _context.SaveChangesAsync();
//            return Ok(existing);
//        }

//        [HttpDelete("{id}")]
//        public async Task<IActionResult> DeleteCoupon(int id)
//        {
//            var coupon = await _context.Coupons.FindAsync(id);
//            if (coupon == null) return NotFound();

//            _context.Coupons.Remove(coupon);
//            await _context.SaveChangesAsync();
//            return NoContent();
//        }

//        [HttpPut("use/{couponId}")]
//        public async Task<IActionResult> UseCoupon(int couponId)
//        {
//            var coupon = await _context.Coupons.FindAsync(couponId);
//            if (coupon == null) return NotFound();
//            if (coupon.IsUsed) return BadRequest("Coupon already used.");

//            coupon.IsUsed = true;
//            await _context.SaveChangesAsync();
//            return Ok(coupon);
//        }

//        [HttpGet("orders/{couponId}")]
//        public async Task<IActionResult> GetOrdersForCoupon(int couponId)
//        {
//            var orders = await _context.Orders
//                .Where(o => o.CouponId == couponId)
//                .ToListAsync();
//            return Ok(orders);
//        }

//        [HttpPost("order")]
//        public async Task<IActionResult> CreateOrder([FromBody] Order order)
//        {
//            if (!ModelState.IsValid) return BadRequest(ModelState);

//            order.CreatedAt = DateTime.UtcNow;
//            _context.Orders.Add(order);
//            await _context.SaveChangesAsync();

//            // Generate coupon for every 5 orders
//            var userOrderCount = await _context.Orders.CountAsync(o => o.UserId == order.UserId);
//            if (userOrderCount % 5 == 0)
//            {
//                var discount = 10.0 * (userOrderCount / 5);
//                var newCoupon = new Coupon
//                {
//                    Code = $"SAVE{discount}",
//                    DiscountPercentage = discount,
//                    IsUsed = false,
//                    UserId = order.UserId,
//                };
//                _context.Coupons.Add(newCoupon);
//                await _context.SaveChangesAsync();
//            }

//            return CreatedAtAction(nameof(CreateOrder), new { id = order.Id }, order);
//        }
//    }
//}

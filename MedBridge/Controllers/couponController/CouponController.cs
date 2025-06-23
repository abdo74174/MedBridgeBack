using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using MoviesApi.models;

namespace CouponSystemApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CouponsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CouponsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Coupon>>> GetAllCoupons()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return Ok(coupons);
        }

        [HttpGet("validate/{code}")]
        public async Task<ActionResult> ValidateCoupon(string code)
        {
            Console.WriteLine($"Validating coupon: {code} for user: {GetUserId()}");
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code);

            if (coupon == null)
            {
                return NotFound("Coupon not found");
            }

            var userId = GetUserId();
            var hasUsed = await _context.UserCouponUsages
                .AnyAsync(uc => uc.CouponId == coupon.Id && uc.UserId == userId);

            if (hasUsed)
            {
                return BadRequest("Coupon already used by this user");
            }

            return Ok(new
            {
                coupon.Id,
                coupon.Code,
                coupon.DiscountPercent,
                coupon.CreatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] Coupon coupon)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            coupon.CreatedAt = DateTime.UtcNow;
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ValidateCoupon), new { code = coupon.Code }, coupon);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] Coupon coupon)
        {
            if (id != coupon.Id || !ModelState.IsValid)
            {
                return BadRequest("Invalid coupon data");
            }

            var existingCoupon = await _context.Coupons.FindAsync(id);
            if (existingCoupon == null)
            {
                return NotFound("Coupon not found");
            }

            existingCoupon.Code = coupon.Code;
            existingCoupon.DiscountPercent = coupon.DiscountPercent;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Coupons.AnyAsync(c => c.Id == id))
                {
                    return NotFound("Coupon not found");
                }
                throw;
            }

            return Ok(coupon);
        }

        [HttpPost("use/{code}")]
        public async Task<IActionResult> UseCoupon(string code)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code);

            if (coupon == null)
            {
                return NotFound("Coupon not found");
            }

            var userId = GetUserId();
            var hasUsed = await _context.UserCouponUsages
                .AnyAsync(uc => uc.CouponId == coupon.Id && uc.UserId == userId);

            if (hasUsed)
            {
                return BadRequest("Coupon already used by this user");
            }

            var usage = new UserCouponUsage
            {
                UserId = userId,
                CouponId = coupon.Id,
                UsedAt = DateTime.UtcNow
            };

            _context.UserCouponUsages.Add(usage);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound("Coupon not found");
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
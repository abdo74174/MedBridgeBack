using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CouponSystemApi.Services
{
    public class CouponServicee : ICouponService
    {
        private readonly ApplicationDbContext _context;

        public CouponServicee(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GetUserId(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User not authenticated");
        }

        public async Task<IActionResult> GetAllCoupons()
        {
            var coupons = await _context.Coupons.ToListAsync();
            return new OkObjectResult(coupons);
        }

        public async Task<IActionResult> ValidateCoupon(string code, string userId)
        {
            Console.WriteLine($"Validating coupon: {code} for user: {userId}");
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code);

            if (coupon == null)
            {
                return new NotFoundObjectResult("Coupon not found");
            }

            var hasUsed = await _context.UserCouponUsages
                .AnyAsync(uc => uc.CouponId == coupon.Id && uc.UserId == userId);

            if (hasUsed)
            {
                return new BadRequestObjectResult("Coupon already used by this user");
            }

            return new OkObjectResult(new
            {
                coupon.Id,
                coupon.Code,
                coupon.DiscountPercent,
                coupon.CreatedAt
            });
        }

        public async Task<IActionResult> CreateCoupon(Coupon coupon)
        {
            if (coupon == null)
            {
                return new BadRequestObjectResult("Invalid coupon data");
            }

            coupon.CreatedAt = DateTime.UtcNow;
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            return new CreatedAtActionResult("ValidateCoupon", "Coupons", new { code = coupon.Code }, coupon);
        }

        public async Task<IActionResult> UpdateCoupon(int id, Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return new BadRequestObjectResult("Invalid coupon data");
            }

            var existingCoupon = await _context.Coupons.FindAsync(id);
            if (existingCoupon == null)
            {
                return new NotFoundObjectResult("Coupon not found");
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
                    return new NotFoundObjectResult("Coupon not found");
                }
                throw;
            }

            return new OkObjectResult(coupon);
        }

        public async Task<IActionResult> UseCoupon(string code, string userId)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code);

            if (coupon == null)
            {
                return new NotFoundObjectResult("Coupon not found");
            }

            var hasUsed = await _context.UserCouponUsages
                .AnyAsync(uc => uc.CouponId == coupon.Id && uc.UserId == userId);

            if (hasUsed)
            {
                return new BadRequestObjectResult("Coupon already used by this user");
            }

            var usage = new UserCouponUsage
            {
                UserId = userId,
                CouponId = coupon.Id,
                UsedAt = DateTime.UtcNow
            };

            _context.UserCouponUsages.Add(usage);
            await _context.SaveChangesAsync();

            return new OkResult();
        }

        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return new NotFoundObjectResult("Coupon not found");
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            return new OkResult();
        }
    }
}
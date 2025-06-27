using Microsoft.AspNetCore.Mvc;
using MoviesApi.models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CouponSystemApi.Services
{
    public interface ICouponService
    {
        string GetUserId(ClaimsPrincipal user);
        Task<IActionResult> GetAllCoupons();
        Task<IActionResult> ValidateCoupon(string code, string userId);
        Task<IActionResult> CreateCoupon(Coupon coupon);
        Task<IActionResult> UpdateCoupon(int id, Coupon coupon);
        Task<IActionResult> UseCoupon(string code, string userId);
        Task<IActionResult> DeleteCoupon(int id);
    }
}
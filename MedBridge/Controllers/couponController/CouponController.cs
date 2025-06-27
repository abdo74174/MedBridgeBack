using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesApi.models;
using CouponSystemApi.Services;
using System.Threading.Tasks;

namespace CouponSystemApi.Controllers
{
   
    [Route("api/[controller]")]
    [ApiController]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public CouponsController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCoupons()
        {
            return await _couponService.GetAllCoupons();
        }

        [HttpGet("validate/{code}")]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            var userId = _couponService.GetUserId(User);
            return await _couponService.ValidateCoupon(code, userId);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] Coupon coupon)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return await _couponService.CreateCoupon(coupon);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] Coupon coupon)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return await _couponService.UpdateCoupon(id, coupon);
        }

        [HttpPost("use/{code}")]
        public async Task<IActionResult> UseCoupon(string code)
        {
            var userId = _couponService.GetUserId(User);
            return await _couponService.UseCoupon(code, userId);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            return await _couponService.DeleteCoupon(id);
        }
    }
}
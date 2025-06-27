using MedBridge.Models;
using MedBridge.Models.PaymentModel;
using MedBridge.Services;
using MedBridge.Services.PaymentService;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpGet("customer/{customerId}/cards")]
        public async Task<IActionResult> GetCustomerCards(int customerId)
        {
            return await _paymentService.GetCustomerCards(customerId);
        }

        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _paymentService.CreatePaymentIntent(request);
        }

        [HttpPost("create-setup-intent")]
        public async Task<IActionResult> CreateSetupIntent([FromBody] SetupIntentRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _paymentService.CreateSetupIntent(request);
        }

        [HttpGet("verify/{paymentIntentId}")]
        public async Task<IActionResult> VerifyPayment(string paymentIntentId)
        {
            return await _paymentService.VerifyPayment(paymentIntentId);
        }

        [HttpPost]
        public async Task<IActionResult> SavePayment([FromBody] Payment payment)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _paymentService.SavePayment(payment);
        }
    }
}
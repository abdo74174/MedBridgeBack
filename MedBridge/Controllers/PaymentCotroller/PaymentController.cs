using Microsoft.AspNetCore.Mvc;
using Stripe;
using MedBridge.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using MoviesApi.models;

namespace MedBridge.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ApplicationDbContext context, ILogger<PaymentController> logger)
        {
            _context = context;
            _logger = logger;

            // Verify Stripe API key is set (set globally in Program.cs)
            if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
            {
                _logger.LogError("Stripe API key is not configured.");
                throw new InvalidOperationException("Stripe API key is not configured.");
            }
        }

        [HttpGet("customer/{customerId}/cards")]
        public async Task<ActionResult> GetCustomerCards(int customerId)
        {
            try
            {
                var user = await _context.users.FindAsync(customerId); // Fixed: 'users' to 'Users'
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {customerId}");
                    return NotFound(new { error = "User not found" });
                }

                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    _logger.LogWarning($"No Stripe customer ID for user: {customerId}");
                    return Ok(new { cards = new List<object>() });
                }

                var service = new PaymentMethodService();
                var options = new PaymentMethodListOptions
                {
                    Customer = user.StripeCustomerId,
                    Type = "card",
                    Limit = 100
                };
                var paymentMethods = await service.ListAsync(options);

                var cards = paymentMethods.Data.Select(pm => new
                {
                    brand = pm.Card.Brand,
                    last4 = pm.Card.Last4
                }).ToList();

                _logger.LogInformation($"Fetched {cards.Count} cards for user: {customerId}");
                return Ok(new { cards });
            }
            catch (StripeException e)
            {
                _logger.LogError($"Stripe error: {e.StripeError.Message}");
                return BadRequest(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError($"Unexpected error: {e.Message}");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }

        [HttpPost("create-payment-intent")]
        public async Task<ActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
        {
            try
            {
                var user = await _context.users.FindAsync(request.CustomerId); // Fixed: 'users' to 'Users'
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {request.CustomerId}");
                    return NotFound(new { error = "User not found" });
                }

                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    var customerService = new CustomerService();
                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email // Optional: Add user details
                    });
                    user.StripeCustomerId = customer.Id;
                    _context.users.Update(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created Stripe customer {customer.Id} for user: {request.CustomerId}");
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = request.Amount,
                    Currency = request.Currency?.ToLower() ?? "egp",
                    Customer = user.StripeCustomerId,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                _logger.LogInformation($"Created payment intent {paymentIntent.Id} for user: {request.CustomerId}");
                return Ok(new
                {
                    client_secret = paymentIntent.ClientSecret,
                    id = paymentIntent.Id
                });
            }
            catch (StripeException e)
            {
                _logger.LogError($"Stripe error: {e.StripeError.Message}");
                return BadRequest(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError($"Unexpected error: {e.Message}");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }

        [HttpPost("create-setup-intent")]
        public async Task<ActionResult> CreateSetupIntent([FromBody] SetupIntentRequest request)
        {
            try
            {
                var user = await _context.users.FindAsync(request.CustomerId); // Fixed: 'users' to 'Users'
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {request.CustomerId}");
                    return NotFound(new { error = "User not found" });
                }

                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    var customerService = new CustomerService();
                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email
                    });
                    user.StripeCustomerId = customer.Id;
                    _context.users.Update(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created Stripe customer {customer.Id} for user: {request.CustomerId}");
                }

                var options = new SetupIntentCreateOptions
                {
                    Customer = user.StripeCustomerId,
                    AutomaticPaymentMethods = new SetupIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };

                var service = new SetupIntentService();
                var setupIntent = await service.CreateAsync(options);

                _logger.LogInformation($"Created setup intent {setupIntent.Id} for user: {request.CustomerId}");
                return Ok(new
                {
                    client_secret = setupIntent.ClientSecret,
                    id = setupIntent.Id
                });
            }
            catch (StripeException e)
            {
                _logger.LogError($"Stripe error: {e.StripeError.Message}");
                return BadRequest(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError($"Unexpected error: {e.Message}");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }

        [HttpGet("verify/{paymentIntentId}")]
        public async Task<ActionResult> VerifyPayment(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);
                _logger.LogInformation($"Verified payment intent {paymentIntentId}: {paymentIntent.Status}");
                return Ok(new { status = paymentIntent.Status });
            }
            catch (StripeException e)
            {
                _logger.LogError($"Stripe error: {e.StripeError.Message}");
                return BadRequest(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError($"Unexpected error: {e.Message}");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }

        [HttpPost]
        public async Task<ActionResult> SavePayment([FromBody] Payment payment)
        {
            try
            {
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Saved payment {payment.PaymentIntentId} for user: {payment.UserId}");
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError($"Database error: {e.Message}");
                return BadRequest(new { error = e.Message });
            }
        }
    }

    public class PaymentIntentRequest
    {
        public long Amount { get; set; }
        public string Currency { get; set; }
        public int CustomerId { get; set; }
    }

    public class SetupIntentRequest
    {
        public int CustomerId { get; set; }
    }
}
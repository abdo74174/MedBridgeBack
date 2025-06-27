using MedBridge.Models;
using MedBridge.Models.PaymentModel;
using MedBridge.Services.PaymentService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesApi.models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedBridge.Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ApplicationDbContext context, ILogger<PaymentService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
            {
                _logger.LogError("Stripe API key is not configured.");
                throw new InvalidOperationException("Stripe API key is not configured.");
            }
        }

        public async Task<IActionResult> GetCustomerCards(int customerId)
        {
            try
            {
                var user = await _context.users.FindAsync(customerId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {CustomerId}", customerId);
                    return new NotFoundObjectResult(new { error = "User not found" });
                }

                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    _logger.LogWarning("No Stripe customer ID for user: {CustomerId}", customerId);
                    return new OkObjectResult(new { cards = new List<object>() });
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

                _logger.LogInformation("Fetched {Count} cards for user: {CustomerId}", cards.Count, customerId);
                return new OkObjectResult(new { cards });
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe error fetching cards for user: {CustomerId}", customerId);
                return new BadRequestObjectResult(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error fetching cards for user: {CustomerId}", customerId);
                return new ObjectResult(new { error = "An unexpected error occurred" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> CreatePaymentIntent(PaymentIntentRequest request)
        {
            try
            {
                if (request.Amount <= 0)
                {
                    _logger.LogWarning("Invalid payment amount: {Amount}", request.Amount);
                    return new BadRequestObjectResult("Payment amount must be greater than zero.");
                }

                var user = await _context.users.FindAsync(request.CustomerId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {CustomerId}", request.CustomerId);
                    return new NotFoundObjectResult(new { error = "User not found" });
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
                    _logger.LogInformation("Created Stripe customer {CustomerId} for user: {UserId}", customer.Id, request.CustomerId);
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

                _logger.LogInformation("Created payment intent {PaymentIntentId} for user: {CustomerId}", paymentIntent.Id, request.CustomerId);
                return new OkObjectResult(new
                {
                    client_secret = paymentIntent.ClientSecret,
                    id = paymentIntent.Id
                });
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe error creating payment intent for user: {CustomerId}", request.CustomerId);
                return new BadRequestObjectResult(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error creating payment intent for user: {CustomerId}", request.CustomerId);
                return new ObjectResult(new { error = "An unexpected error occurred" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> CreateSetupIntent(SetupIntentRequest request)
        {
            try
            {
                var user = await _context.users.FindAsync(request.CustomerId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {CustomerId}", request.CustomerId);
                    return new NotFoundObjectResult(new { error = "User not found" });
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
                    _logger.LogInformation("Created Stripe customer {CustomerId} for user: {UserId}", customer.Id, request.CustomerId);
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

                _logger.LogInformation("Created setup intent {SetupIntentId} for user: {CustomerId}", setupIntent.Id, request.CustomerId);
                return new OkObjectResult(new
                {
                    client_secret = setupIntent.ClientSecret,
                    id = setupIntent.Id
                });
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe error creating setup intent for user: {CustomerId}", request.CustomerId);
                return new BadRequestObjectResult(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error creating setup intent for user: {CustomerId}", request.CustomerId);
                return new ObjectResult(new { error = "An unexpected error occurred" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> VerifyPayment(string paymentIntentId)
        {
            try
            {
                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    _logger.LogWarning("Payment intent ID is required");
                    return new BadRequestObjectResult("Payment intent ID is required.");
                }

                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);
                _logger.LogInformation("Verified payment intent {PaymentIntentId}: {Status}", paymentIntentId, paymentIntent.Status);
                return new OkObjectResult(new { status = paymentIntent.Status });
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe error verifying payment intent: {PaymentIntentId}", paymentIntentId);
                return new BadRequestObjectResult(new { error = e.Message });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error verifying payment intent: {PaymentIntentId}", paymentIntentId);
                return new ObjectResult(new { error = "An unexpected error occurred" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> SavePayment(Payment payment)
        {
            try
            {
                if (payment == null || string.IsNullOrEmpty(payment.PaymentIntentId) || payment.UserId == 0)
                {
                    _logger.LogWarning("Invalid payment data: PaymentIntentId or UserId missing");
                    return new BadRequestObjectResult("Invalid payment data.");
                }

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved payment {PaymentIntentId} for user: {UserId}", payment.PaymentIntentId, payment.UserId);
                return new OkObjectResult(null);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Database error saving payment for user: {UserId}", payment.UserId);
                return new BadRequestObjectResult(new { error = e.Message });
            }
        }
    }
}
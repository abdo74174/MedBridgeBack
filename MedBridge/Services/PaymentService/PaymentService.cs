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
                if (customerId <= 0)
                {
                    _logger.LogWarning("Invalid customer ID: {CustomerId}", customerId);
                    return new BadRequestObjectResult(new { error = "Invalid customer ID" });
                }

                var user = await _context.users.FirstOrDefaultAsync(u => u.Id == customerId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {CustomerId}", customerId);
                    return new NotFoundObjectResult(new { error = "User not found" });
                }

                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    _logger.LogInformation("No Stripe customer ID for user: {CustomerId}. Creating new Stripe customer.", customerId);
                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        _logger.LogWarning("User email is empty for ID: {CustomerId}", customerId);
                        return new BadRequestObjectResult(new { error = "User email is required to create a Stripe customer" });
                    }

                    var customerService = new CustomerService();
                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email,
                        Name = user.Name ?? "Unknown"
                    });
                    user.StripeCustomerId = customer.Id;
                    _context.users.Update(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created Stripe customer {StripeCustomerId} for user: {CustomerId}", customer.Id, customerId);
                }

                var service = new PaymentMethodService();
                var options = new PaymentMethodListOptions
                {
                    Customer = user.StripeCustomerId,
                    Type = "card",
                    Limit = 100
                };
                var paymentMethods = await service.ListAsync(options);

                var

 cards = paymentMethods.Data.Select(pm => new
 {
     id = pm.Id,
     brand = pm.Card.Brand,
     last4 = pm.Card.Last4,
     expMonth = pm.Card.ExpMonth,
     expYear = pm.Card.ExpYear
 }).ToList();

                _logger.LogInformation("Fetched {Count} cards for user: {CustomerId}", cards.Count, customerId);
                return new OkObjectResult(new { cards });
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe error fetching cards for user: {CustomerId}. StripeError: {StripeError}", customerId, e.StripeError?.Message);
                return new BadRequestObjectResult(new { error = $"Stripe error: {e.StripeError?.Message ?? e.Message}" });
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "Database error fetching cards for user: {CustomerId}. InnerException: {InnerException}", customerId, e.InnerException?.Message);
                return new ObjectResult(new { error = "Database error occurred while fetching cards" }) { StatusCode = 500 };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error fetching cards for user: {CustomerId}. InnerException: {InnerException}", customerId, e.InnerException?.Message);
                return new ObjectResult(new { error = "An unexpected error occurred while fetching cards" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> CreatePaymentIntent(PaymentIntentRequest request)
        {
            try
            {
                if (request == null || request.Amount <= 0 || request.CustomerId <= 0)
                {
                    _logger.LogWarning("Invalid payment intent request: Amount={Amount}, CustomerId={CustomerId}", request?.Amount, request?.CustomerId);
                    return new BadRequestObjectResult(new { error = "Invalid payment amount or customer ID" });
                }

                var user = await _context.users.FirstOrDefaultAsync(u => u.Id == request.CustomerId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {CustomerId}", request.CustomerId);
                    return new NotFoundObjectResult(new { error = "User not found" });
                }

                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    _logger.LogInformation("No Stripe customer ID for user: {CustomerId}. Creating new Stripe customer.", request.CustomerId);
                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        _logger.LogWarning("User email is empty for ID: {CustomerId}", request.CustomerId);
                        return new BadRequestObjectResult(new { error = "User email is required to create a Stripe customer" });
                    }

                    var customerService = new CustomerService();
                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email,
                        Name = user.Name ?? "Unknown"
                    });
                    user.StripeCustomerId = customer.Id;
                    _context.users.Update(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created Stripe customer {StripeCustomerId} for user: {CustomerId}", customer.Id, request.CustomerId);
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
                _logger.LogError(e, "Stripe error creating payment intent for user: {CustomerId}. StripeError: {StripeError}", request?.CustomerId, e.StripeError?.Message);
                return new BadRequestObjectResult(new { error = $"Stripe error: {e.StripeError?.Message ?? e.Message}" });
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "Database error creating payment intent for user: {CustomerId}. InnerException: {InnerException}", request?.CustomerId, e.InnerException?.Message);
                return new ObjectResult(new { error = "Database error occurred while creating payment intent" }) { StatusCode = 500 };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error creating payment intent for user: {CustomerId}. InnerException: {InnerException}", request?.CustomerId, e.InnerException?.Message);
                return new ObjectResult(new { error = "An unexpected error occurred while creating payment intent" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> CreateSetupIntent(SetupIntentRequest request)
        {
            try
            {
                if (request == null || request.CustomerId <= 0)
                {
                    _logger.LogWarning("Invalid setup intent request: CustomerId={CustomerId}", request?.CustomerId);
                    return new BadRequestObjectResult(new { error = "Invalid customer ID" });
                }

                var user = await _context.users.FirstOrDefaultAsync(u => u.Id == request.CustomerId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {CustomerId}", request.CustomerId);
                    return new NotFoundObjectResult(new { error = "User not found" });
                }

                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    _logger.LogInformation("No Stripe customer ID for user: {CustomerId}. Creating new Stripe customer.", request.CustomerId);
                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        _logger.LogWarning("User email is empty for ID: {CustomerId}", request.CustomerId);
                        return new BadRequestObjectResult(new { error = "User email is required to create a Stripe customer" });
                    }

                    var customerService = new CustomerService();
                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email,
                        Name = user.Name ?? "Unknown"
                    });
                    user.StripeCustomerId = customer.Id;
                    _context.users.Update(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created Stripe customer {StripeCustomerId} for user: {CustomerId}", customer.Id, request.CustomerId);
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
                _logger.LogError(e, "Stripe error creating setup intent for user: {CustomerId}. StripeError: {StripeError}", request?.CustomerId, e.StripeError?.Message);
                return new BadRequestObjectResult(new { error = $"Stripe error: {e.StripeError?.Message ?? e.Message}" });
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "Database error creating setup intent for user: {CustomerId}. InnerException: {InnerException}", request?.CustomerId, e.InnerException?.Message);
                return new ObjectResult(new { error = "Database error occurred while creating setup intent" }) { StatusCode = 500 };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error creating setup intent for user: {CustomerId}. InnerException: {InnerException}", request?.CustomerId, e.InnerException?.Message);
                return new ObjectResult(new { error = "An unexpected error occurred while creating setup intent" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> VerifyPayment(string paymentIntentId)
        {
            try
            {
                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    _logger.LogWarning("Payment intent ID is required");
                    return new BadRequestObjectResult(new { error = "Payment intent ID is required" });
                }

                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);
                _logger.LogInformation("Verified payment intent {PaymentIntentId}: {Status}", paymentIntentId, paymentIntent.Status);
                return new OkObjectResult(new { status = paymentIntent.Status });
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe error verifying payment intent: {PaymentIntentId}. StripeError: {StripeError}", paymentIntentId, e.StripeError?.Message);
                return new BadRequestObjectResult(new { error = $"Stripe error: {e.StripeError?.Message ?? e.Message}" });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error verifying payment intent: {PaymentIntentId}. InnerException: {InnerException}", paymentIntentId, e.InnerException?.Message);
                return new ObjectResult(new { error = "An unexpected error occurred while verifying payment" }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> SavePayment(Payment payment)
        {
            try
            {
                if (payment == null || string.IsNullOrEmpty(payment.PaymentIntentId) || payment.UserId <= 0)
                {
                    _logger.LogWarning("Invalid payment data: PaymentIntentId={PaymentIntentId}, UserId={UserId}", payment?.PaymentIntentId, payment?.UserId);
                    return new BadRequestObjectResult(new { error = "Invalid payment data" });
                }

                var user = await _context.users.FindAsync(payment.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for payment: {UserId}", payment.UserId);
                    return new NotFoundObjectResult(new { error = "User not found" });
                }

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved payment {PaymentIntentId} for user: {UserId}", payment.PaymentIntentId, payment.UserId);
                return new OkObjectResult(new { message = "Payment saved successfully" });
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "Database error saving payment for user: {UserId}. InnerException: {InnerException}", payment?.UserId, e.InnerException?.Message);
                return new ObjectResult(new { error = "Database error occurred while saving payment" }) { StatusCode = 500 };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error saving payment for user: {UserId}. InnerException: {InnerException}", payment?.UserId, e.InnerException?.Message);
                return new ObjectResult(new { error = "An unexpected error occurred while saving payment" }) { StatusCode = 500 };
            }
        }
    }
}
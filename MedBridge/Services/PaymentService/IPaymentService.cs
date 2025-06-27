using MedBridge.Models;
using MedBridge.Models.PaymentModel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services.PaymentService
{
    public interface IPaymentService
    {
        Task<IActionResult> GetCustomerCards(int customerId);
        Task<IActionResult> CreatePaymentIntent(PaymentIntentRequest request);
        Task<IActionResult> CreateSetupIntent(SetupIntentRequest request);
        Task<IActionResult> VerifyPayment(string paymentIntentId);
        Task<IActionResult> SavePayment(Payment payment);
    }
}
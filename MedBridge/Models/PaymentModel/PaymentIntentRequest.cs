namespace MedBridge.Models.PaymentModel
{
    public class PaymentIntentRequest
    {
        public long Amount { get; set; }
        public string Currency { get; set; }
        public int CustomerId { get; set; }
    }
}

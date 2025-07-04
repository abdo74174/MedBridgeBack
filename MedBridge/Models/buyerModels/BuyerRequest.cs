namespace MedbridgeApi.Models
{
    public class BuyerRequest
    {
        public int Id { get; set; }
        public string FacilityLicenseNumber { get; set; } = string.Empty;
        public string CommercialRegisterNumber { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public string EdaLicenseNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
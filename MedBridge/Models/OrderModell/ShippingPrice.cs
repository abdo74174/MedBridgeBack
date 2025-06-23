namespace MedicalStoreAPI.Models;

public class ShippingPrice
{
    public int Id { get; set; }
    public string Governorate { get; set; } = string.Empty;
    public double Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
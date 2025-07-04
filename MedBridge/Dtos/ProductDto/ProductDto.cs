using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedBridge.Dtos.Product
{
    public class ProductDto
    {
        public int ProductId { get; set; }

        [Required, StringLength(255)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsNew { get; set; }
        public bool Donation { get; set; } = false;
        public string serialNumber { get; set; }

        public double Discount { get; set; }

        // Foreign keys
        public int SubCategoryId { get; set; }
        public int CategoryId { get; set; }

        // List of image URLs
        public List<string> Images { get; set; } = new List<string>(); // Already correct, kept for completeness

        public int StockQuantity { get; set; }
        public int UserId { get; set; }
    }
}
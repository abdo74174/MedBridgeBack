using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

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

        public double Discount { get; set; }

        // Foreign keys
        public int SubCategoryId { get; set; }
        public int CategoryId { get; set; }

        // List of image files
        public List<string> Images { get; set; }


        public int StockQuantity { get; set; }
        public int UserId { get; set; }
    }
}

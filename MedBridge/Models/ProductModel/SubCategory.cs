using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models.ProductModels
{
    public class subCategory
    {
        [Key]
        public int SubCategoryId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<ProductModel> Products { get; set; } = new List<ProductModel>();
    }
}

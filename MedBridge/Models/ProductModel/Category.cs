using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models.ProductModels
{
    public class Category
    {

        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }

        public ICollection<subCategory> SubCategories { get; set; } = new List<subCategory>();
        public ICollection<ProductModel> Products { get; set; } = new List<ProductModel>();



    }
}

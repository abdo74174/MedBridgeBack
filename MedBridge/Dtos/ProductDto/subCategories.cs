using MedBridge.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedBridge.Dtos.Product
{
    public class subCategoriesDto
    {

        public int SubCategoryId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public IFormFile? Image  { get; set; }

        public int CategoryId { get; set; }
     
    }
}

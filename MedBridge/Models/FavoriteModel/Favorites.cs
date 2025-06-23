using MedBridge.Models.ProductModels;

namespace MedBridge.Models
{
    public class Favourite
    {
        public int Id { get; set; }

        public string UserId { get; set; }    

        public int ProductId { get; set; }    

        public virtual ProductModel Product { get; set; }
    }
}

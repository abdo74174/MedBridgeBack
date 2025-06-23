using MedBridge.Models.ProductModels;

namespace MedBridge.Models
{
    public class CartItem
    {
       
            public int Id { get; set; }  // معرف العنصر في السلة
            public int CartId { get; set; }  // معرف السلة اللي تابع ليها
            public int ProductId { get; set; }  // معرف المنتج اللي مضاف للسلة
            public int Quantity { get; set; }  // الكمية المطلوبة

            // العلاقات بين الجداول
            public ProductModel Product { get; set; }  // العلاقة مع جدول المنتجات
            public CartModel Cart { get; set; }


    }
}

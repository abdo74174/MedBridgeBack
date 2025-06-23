namespace MedBridge.Models
{
    public class CartModel
    {
       
            public int Id { get; set; }  // معرف السلة
            public string UserId { get; set; }  // معرف المستخدم اللي يملك السلة
            public List<CartItem> CartItems { get; set; } = new List<CartItem>();  // العناصر داخل السلة
        
    }
}

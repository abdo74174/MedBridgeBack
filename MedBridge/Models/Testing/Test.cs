using MedBridge.Models.UsersModel; // تأكد من استيراد المسار الصحيح

namespace MedBridge.Models.Testing
{
    public class DeviceTokens
    {
        public int Id { get; set; } // Primary key
        public string Token { get; set; }

        public int UserId { get; set; } // Foreign Key

        // ✅ Navigation Property لإكمال العلاقة
        public User User { get; set; }
    }
}

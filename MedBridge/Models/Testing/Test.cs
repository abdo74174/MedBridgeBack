namespace MedBridge.Models.Testing
{
    public class DeviceTokens
    {
        public int Id { get; set; } // Primary key
        public string Token { get; set; }
        public int UserId { get; set; } // Associate token with a user
    }
}
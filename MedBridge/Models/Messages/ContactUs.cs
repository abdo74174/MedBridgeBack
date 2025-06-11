using System;

namespace MedBridge.Models.Messages
{
    public class ContactUs
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
namespace MedBridge.Models.Messages
{
    public class ContactUs
    {
        public int Id { get; set; }
        public string ProblemType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
namespace MedBridge.Dtos
{
    public class ContactUsDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
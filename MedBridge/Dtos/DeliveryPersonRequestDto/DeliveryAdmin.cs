namespace GraduationProject.Core.Dtos
{
    public class DeliveryPersonRequestAdminDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
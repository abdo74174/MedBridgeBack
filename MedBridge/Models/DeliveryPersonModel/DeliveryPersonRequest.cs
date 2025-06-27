using MedBridge.Models;

namespace GraduationProject.Core.Entities
{
    public class DeliveryPerson
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? CardNumber { get; set; }
        public string RequestStatus { get; set; } = "Pending";
        public bool IsAvailable { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public User User { get; set; } // Navigation property
    }
}
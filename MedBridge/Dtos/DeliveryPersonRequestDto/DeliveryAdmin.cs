namespace GraduationProject.Core.Dtos
{
    public class DeliveryPersonRequestAdminDto
    {
        public int Id { get; set; } // Add Id
        public string Name { get; set; } // Add Name
        public string Email { get; set; } // Add Email
        public string Phone { get; set; }
        public string Address { get; set; }
        public string CardNumber { get; set; }
        public string RequestStatus { get; set; }
        public DateTime CreatedAt { get; set; } // Add CreatedAt
    }
}
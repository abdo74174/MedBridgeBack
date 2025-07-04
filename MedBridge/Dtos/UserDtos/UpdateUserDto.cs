namespace MedBridge.Dtos
{
    public class UpdateUserForm
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? MedicalSpecialist { get; set; }
        public string? ProfileImage { get; set; } // Cloudinary URL as string
    }
}
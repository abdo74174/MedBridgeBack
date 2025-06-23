namespace MedBridge.Models.GoogLe_signIn
{
    public class GoogleProfileCompletionRequest
    {
        public string Email { get; set; }
        public string? Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string? MedicalSpecialist { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
namespace MedBridge.Models.GoogLe_signIn
{
    public class GoogleLoginResponse
    {
        public string Status { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public int Id { get; set; }
        public string KindOfWork { get; set; }
        public string MedicalSpecialist { get; set; }
        public bool IsAdmin { get; set; }
    }
}


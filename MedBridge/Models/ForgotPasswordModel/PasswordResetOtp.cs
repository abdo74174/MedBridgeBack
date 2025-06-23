using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models.ForgotPassword
{
    public class PasswordResetOtp
    {
        public int Id { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Otp { get; set; }
        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}
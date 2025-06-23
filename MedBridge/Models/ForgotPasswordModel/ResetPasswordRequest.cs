using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models.ForgotPassword
{
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Otp { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "The password must contain at least one number, one letter, and one special character.")]
        public string NewPassword { get; set; }
    }
}
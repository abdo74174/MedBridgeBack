using System.ComponentModel.DataAnnotations;

namespace MedBridge.Dtos.UserDtos
{
    public class VerifyOtpDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }

        [Required]
        [RegularExpression(
            @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$",
            ErrorMessage = "Password must contain 8+ chars, 1 letter, 1 number, and 1 special character")]
        public string NewPassword { get; set; }
    }
}

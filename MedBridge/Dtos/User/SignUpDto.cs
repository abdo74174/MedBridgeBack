using System.ComponentModel.DataAnnotations;

namespace MedBridge.Dtos
{
    public class SignUpDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "The Email address is not valid.")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "The password must contain at least one number, one letter, and one special character.")]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; }

        public string? Phone { get; set; }
        public string? MedicalSpecialist { get; set; }
        public string? Address { get; set; }
    }
}
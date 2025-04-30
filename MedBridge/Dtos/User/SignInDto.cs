using System.ComponentModel.DataAnnotations;

namespace MedBridge.Dtos
{
    public class SignInDto
    {
        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "The Email address is not valid.")]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
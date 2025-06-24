using System.ComponentModel.DataAnnotations;

namespace MedBridge.Dtos.UserDtos
{
    public class SendOtpDto
    {
        [Required(ErrorMessage = "The email field is required.")]
        [EmailAddress(ErrorMessage = "The email address is not valid.")]
        public string Email { get; set; }
    }
}

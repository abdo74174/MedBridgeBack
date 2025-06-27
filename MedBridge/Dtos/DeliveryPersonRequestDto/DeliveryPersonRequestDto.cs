using System.ComponentModel.DataAnnotations;

namespace GraduationProject.Core.Dtos
{
    public class DeliveryPersonRequestDto
    {
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "The Phone number is not valid.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Card number is required.")]
        public string CardNumber { get; set; } = string.Empty;

        public string? RequestStatus { get; set; }
        public bool? IsAvailable { get; set; }
        public int? UserId { get; set; } // Optional: Provided if user exists
        public int? DeliveryPersonId { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "The Email address is not valid.")]
        public string Email { get; set; } = string.Empty;
    }
}
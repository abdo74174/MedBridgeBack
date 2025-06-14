using System.ComponentModel.DataAnnotations;

namespace GraduationProject.Core.Dtos
{
    public class DeliveryPersonRequestDto
    {
        [Phone(ErrorMessage = "The Phone number is not valid.")]
        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Card number is required.")]
        public string CardNumber { get; set; } = string.Empty;

        public string? RequestStatus { get; set; }
    }
}
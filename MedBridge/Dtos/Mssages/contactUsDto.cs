using MedBridge.Models;
using System.ComponentModel.DataAnnotations;

namespace MedBridge.Dtos
{
    public class ContactUsDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Message { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
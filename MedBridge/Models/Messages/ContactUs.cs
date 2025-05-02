using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models.Messages
{
    public class ContactUs
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Message { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime created { get; set; } = DateTime.UtcNow;
    }
}
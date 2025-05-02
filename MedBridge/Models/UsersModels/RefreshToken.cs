using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}
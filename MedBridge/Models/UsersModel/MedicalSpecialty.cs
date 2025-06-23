using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models
{
    public class MedicalSpecialty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
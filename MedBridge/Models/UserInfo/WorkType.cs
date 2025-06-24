using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models.UserInfo
{
    public class WorkType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
    }
}
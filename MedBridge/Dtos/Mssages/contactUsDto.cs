

using MedBridge.Models;
using System.ComponentModel.DataAnnotations;

namespace MedBridge.Dtos.Mssages
{
    public class contactUsDto
    {

        [Key]
        public int MessageId { get; set; }

        public String Message { get; set; }


        public int UserId { get; set; }
        public User? User { get; set; }
    }
}

using MedBridge.Models;
using System.ComponentModel.DataAnnotations;

namespace GraduationProject.Core.Entities
{
    public class DeliveryPerson : User
    {
        public string? RequestStatus { get; set; } = "Pending";
        public bool? IsAvailable { get; set; } = false;
        public string? CardNumber { get; set; }
        public int userId { get; set; }
    }
}
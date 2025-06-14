//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using MedBridge.Models;
//using MedBridge.Models.OrderModels;
//using Microsoft.EntityFrameworkCore;

//namespace MedBridge.Modelss
//{
//    public class DeliveryPerson : User
//    {
        

//        public bool IsAvailable { get; set; } = true;

//        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


//        public ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
//    }

//}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MedBridge.Models.ProductModels;
using Microsoft.EntityFrameworkCore;

namespace MedBridge.Models.OrderModels
{
    public enum OrderStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public bool IsDeleted { get; set; } = false; // Added for soft delete
        public int? CouponId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<OrderItem>? OrderItems { get; set; }

        [Precision(18, 2)]
        public decimal TotalPrice { get; set; }
        //public object Id { get; internal set; }
    }
}
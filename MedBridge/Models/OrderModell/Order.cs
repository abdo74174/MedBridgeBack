﻿using GraduationProject.Core.Entities;
using MedBridge.Models.OrderModels;
using MedBridge.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

public class Order
{

    public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled, Assigned, AwaitingUserConfirmation, AwaitingDeliveryConfirmation }
    [Key]
    public int OrderId { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    public int? DeliveryPersonId { get; set; }
    public DeliveryPerson? DeliveryPerson { get; set; }

    [Required]
    public string Address { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public bool IsDeleted { get; set; } = false;
    public int? CouponId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem>? OrderItems { get; set; }

    [Precision(18, 2)]
    public decimal TotalPrice { get; set; }

    public bool UserConfirmedShipped { get; set; } = false;
    public bool DeliveryPersonConfirmedShipped { get; set; }
}
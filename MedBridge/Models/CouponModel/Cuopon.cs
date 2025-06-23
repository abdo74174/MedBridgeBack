using System.ComponentModel.DataAnnotations;

public class Coupon
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public double DiscountPercent { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class UserCouponUsage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int CouponId { get; set; }

    public Coupon Coupon { get; set; } = null!;

    public DateTime UsedAt { get; set; }
}
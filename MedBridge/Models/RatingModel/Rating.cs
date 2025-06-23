namespace RatingApi.Models;

public class Rating
{
    public int Id { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int RatingValue { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
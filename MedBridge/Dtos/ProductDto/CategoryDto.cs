﻿public class CategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string? ImageUrl { get; set; } // Changed from IFormFile to string
}
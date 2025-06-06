﻿public class SignUpDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsAdmin { get; set; } = false;
}

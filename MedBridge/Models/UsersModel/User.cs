﻿using MedBridge.Models.Messages;
using MedBridge.Models.ProductModels;
using MedBridge.Models.Testing;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedBridge.Models
{
    public class User
    {
        public enum UserStatus
        {
            Active,
            Blocked,
            Deactivated
        }

        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        [Required]
        public string Name { get; set; }

        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "The Email address is not valid.")]
        public string Email { get; set; }

        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }

        [NotMapped]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "The password must contain at least one number, one letter, and one special character.")]
        public string? Password
        {
            get => string.Empty;
            set
            {
                // Implement proper password hashing here
                // Example:
                // PasswordSalt = GenerateSalt();
                // PasswordHash = HashPassword(value, PasswordSalt);
            }
        }

        [NotMapped]
        public string? ConfirmPassword { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public string? Phone { get; set; }
        public string? MedicalSpecialist { get; set; }
        public string? Address { get; set; }
        public bool ? isBuyer { get; set; } = false;
        public string ProfileImage { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string KindOfWork { get; set; } = "Doctor";
        public string   StripeCustomerId { get; set; }
        public bool IsAdmin { get; set; } = false;
        public UserStatus Status { get; set; } = UserStatus.Active;
        public ICollection<DeviceTokens> DeviceTokens { get; set; } = new List<DeviceTokens>();

        public ICollection<ProductModel> Products { get; set; } = new List<ProductModel>();
        public ICollection<ContactUs> ContactUs { get; set; } = new List<ContactUs>();
    }
}
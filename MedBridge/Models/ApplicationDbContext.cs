using MedBridge.Models;
using MedBridge.Models.ForgotPassword;
using MedBridge.Models.Messages;
using MedBridge.Models.OrderModels;
using MedBridge.Models.ProductModels;
using Microsoft.EntityFrameworkCore;
using RatingApi.Models;

namespace MoviesApi.models
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions <ApplicationDbContext > options) : base (options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<OrderItem>()
           .HasOne(oi => oi.Order)
           .WithMany(o => o.OrderItems)
           .HasForeignKey(oi => oi.OrderId)
           .OnDelete(DeleteBehavior.Cascade); // Cascade delete for Order

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany() // No navigation property in ProductModel
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.NoAction); // No cascade for Product

            // ✅ منع الحذف التتابعي بين `ProductModel` و `SubCategory`
            modelBuilder.Entity<ProductModel>()
                .HasOne(p => p.SubCategory)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SubCategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<subCategory>()
            .HasOne(s => s.Category)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            // Allow cascade delete for Category -> ProductModel relationship
            modelBuilder.Entity<ProductModel>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete


            modelBuilder.Entity<WorkType>().HasData(
            new WorkType { Id = 1, Name = "Doctor" },
            new WorkType { Id = 2, Name = "Merchant" },
            new WorkType { Id = 3, Name = "MedicalTrader" }
        );

            // Seed initial MedicalSpecialties
            modelBuilder.Entity<MedicalSpecialty>().HasData(
                new MedicalSpecialty { Id = 1, Name = "Cardiology" },
                new MedicalSpecialty { Id = 2, Name = "Neurology" },
                new MedicalSpecialty { Id = 3, Name = "Pediatrics" },
                new MedicalSpecialty { Id = 4, Name = "Orthopedics" },
                new MedicalSpecialty { Id = 5, Name = "Dermatology" }
            );

            base.OnModelCreating(modelBuilder);

      


        }





        public DbSet<Category>Categories { get; set; }

        public DbSet<subCategory> subCategories { get; set; }

        public DbSet<ProductModel> Products { get; set; }

        public DbSet<User> users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<CartModel> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<ContactUs> ContactUs { get; set; }

        public DbSet<Favourite> Favourites { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }

        public DbSet<PasswordResetOtp> PasswordResetOtp { get; set; }


        public DbSet<WorkType> WorkType { get; set; }


        public DbSet<Order> Orders { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        //public DbSet<Coupon> Coupons { get; set; }
        public DbSet<MedicalSpecialty> MedicalSpecialties { get; set; }
    }
}

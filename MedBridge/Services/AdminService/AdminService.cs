using MedBridge.Models;
using MedBridge.Models.Messages;
using MedBridge.Models.ProductModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MedBridge.Models.User;

namespace MedBridge.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.users
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.MedicalSpecialist,
                    u.Address,
                    u.ProfileImage,
                    u.CreatedAt,
                    u.KindOfWork,
                    u.IsAdmin,
                    u.Status,
                    Products = u.Products.Select(p => new
                    {
                        p.ProductId,
                        p.Name,
                        p.Description
                    }).ToList(),
                    ContactUsMessages = u.ContactUs.Select(c => new
                    {
                        c.Id,
                        c.Message,
                        c.CreatedAt
                    }).ToList()
                })
                .ToListAsync();

            return new OkObjectResult(users);
        }

        public async Task<IActionResult> AddAdmin(int id)
        {
            var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Id == id);

            if (existingUser == null)
                return new NotFoundObjectResult("User not found.");

            if (existingUser.IsAdmin)
                return new BadRequestObjectResult("Admin already exists.");

            existingUser.IsAdmin = true;
            await _context.SaveChangesAsync();

            return new OkObjectResult("User promoted to admin.");
        }

        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try
            {
                var admin = await _context.users.FindAsync(id);
                if (admin == null || !admin.IsAdmin)
                    return new NotFoundObjectResult("Admin not found.");

                if (admin.Id == 1)
                    return new BadRequestObjectResult("Main admin cannot be deleted.");

                admin.IsAdmin = false;
                await _context.SaveChangesAsync();

                return new OkObjectResult("Admin deleted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new ObjectResult("An error occurred: " + ex.Message) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> BlockUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null || user.IsAdmin)
                return new BadRequestObjectResult("Invalid user.");

            user.Status = UserStatus.Blocked;
            await _context.SaveChangesAsync();
            return new OkObjectResult("User blocked.");
        }

        public async Task<IActionResult> UnBlockUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null || user.IsAdmin)
                return new BadRequestObjectResult("Invalid user.");

            user.Status = UserStatus.Active;
            await _context.SaveChangesAsync();
            return new OkObjectResult("User Un blocked.");
        }

        public async Task<IActionResult> ActivateUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null || user.IsAdmin)
                return new BadRequestObjectResult("Invalid user.");

            user.Status = UserStatus.Active;
            await _context.SaveChangesAsync();
            return new OkObjectResult("User deactivated.");
        }

        public async Task<IActionResult> DeactivateUser(int id)
        {
            var user = await _context.users.FindAsync(id);
            if (user == null || user.IsAdmin)
                return new BadRequestObjectResult("Invalid user.");

            user.Status = UserStatus.Deactivated;
            await _context.SaveChangesAsync();
            return new OkObjectResult("User deactivated.");
        }
    }
}
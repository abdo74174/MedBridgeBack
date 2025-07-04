using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Google.Apis.Auth;
using MedBridge.Dtos;
using MedBridge.Dtos.AddProfileImagecsDtoUser;
using MedBridge.Models;
using MedBridge.Models.GoogLe_signIn;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MoviesApi.models;
using Stripe;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static MedBridge.Models.User;

namespace MedBridge.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly Cloudinary _cloudinary;
        private readonly IGoogleSignIn _googleSignIn;
        private readonly List<string> _allowedExtensions = new List<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg", ".ico", ".heif"
        };
        private readonly double _maxAllowedImageSize;

        public UserService(
            ApplicationDbContext context,
            IConfiguration configuration,
            Cloudinary cloudinary,
            ILogger<UserService> logger,
            IMemoryCache memoryCache,
            IGoogleSignIn googleSignIn)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _cloudinary = cloudinary;
            _googleSignIn = googleSignIn ?? throw new ArgumentNullException(nameof(googleSignIn));
            _maxAllowedImageSize = _configuration.GetValue<double>("ImageSettings:MaxAllowedImageSize", 10 * 1024 * 1024);
        }

        public async Task<bool> IsUserAdminAsync(string email)
        {
            var user = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null && user.IsAdmin;
        }

        public async Task<IActionResult> SignUpAsync(SignUpDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email) ||
                    string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                {
                    _logger.LogWarning("Missing required fields for signup: {Email}", dto.Email);
                    return new BadRequestObjectResult(new { message = "Name, email, password, and confirm password are required." });
                }

                if (dto.Password != dto.ConfirmPassword)
                {
                    _logger.LogWarning("Passwords do not match for signup: {Email}", dto.Email);
                    return new BadRequestObjectResult(new { message = "Passwords do not match." });
                }

                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Email already exists: {Email}", dto.Email);
                    return new BadRequestObjectResult(new { message = "Email already exists." });
                }

                var (passwordHash, passwordSalt) = PasswordHasher.CreatePasswordHash(dto.Password);

                var customerService = new CustomerService();
                var stripeCustomer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = dto.Email,
                    Name = dto.Name,
                    Phone = dto.Phone,
                    Description = "MedBridge User"
                });

                var user = new User
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Phone = dto.Phone,
                    Address = dto.Address,
                    CreatedAt = DateTime.UtcNow,
                    KindOfWork = "Doctor",
                    MedicalSpecialist = null,
                    ProfileImage = "",
                    IsAdmin = dto.IsAdmin,
                    StripeCustomerId = stripeCustomer.Id
                };

                _context.users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User registered and Stripe customer created: {Email}", user.Email);
                return new OkObjectResult(new
                {
                    message = "User registered successfully!",
                    id = user.Id,
                    stripeCustomerId = user.StripeCustomerId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SignUp for {Email}", dto.Email);
                return new ObjectResult(new { message = "An error occurred during signup." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> SignInAsync(SignInDto dto)
        {
            try
            {
                var cacheKey = $"LoginAttempts_{dto.Email}";
                var attempts = _memoryCache.Get<int>(cacheKey);

                if (attempts >= 5)
                {
                    _logger.LogWarning("Too many failed login attempts for {Email}", dto.Email);
                    return new BadRequestObjectResult(new { message = "Too many failed attempts. Try again after 15 minutes." });
                }

                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (existingUser == null)
                {
                    _memoryCache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(15));
                    _logger.LogWarning("Invalid email for signin: {Email}", dto.Email);
                    return new BadRequestObjectResult(new { message = "Invalid email or password." });
                }

                bool isPasswordValid = PasswordHasher.VerifyPasswordHash(dto.Password, existingUser.PasswordHash, existingUser.PasswordSalt);
                if (!isPasswordValid)
                {
                    _memoryCache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(15));
                    _logger.LogWarning("Invalid password for signin: {Email}", dto.Email);
                    return new BadRequestObjectResult(new { message = "Invalid email or password." });
                }

                var (tokenString, newRefreshToken) = await GenerateTokensAsync(existingUser);

                _memoryCache.Remove(cacheKey);
                _logger.LogInformation("User signed in: {Email}", dto.Email);
                return new OkObjectResult(new
                {
                    id = existingUser.Id,
                    token = tokenString,
                    expiresIn = 3600,
                    refreshToken = newRefreshToken.Token,
                    kindOfWork = existingUser.KindOfWork,
                    medicalSpecialist = existingUser.MedicalSpecialist,
                    isAdmin = existingUser.IsAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SignIn for {Email}", dto.Email);
                return new ObjectResult(new { message = "An error occurred during signin." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> SignInWithGoogleAsync(GoogleSignInRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
                {
                    _logger.LogWarning("Invalid Google ID token");
                    return new BadRequestObjectResult(new { message = "Invalid Google ID token" });
                }

                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
                var email = payload.Email;
                var name = payload.Name;

                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser == null)
                {
                    _logger.LogInformation("New Google user requires profile completion: {Email}", email);
                    return new OkObjectResult(new
                    {
                        id = (string)null,
                        email,
                        name,
                        requiresProfileCompletion = true
                    });
                }

                var (tokenString, newRefreshToken) = await GenerateTokensAsync(existingUser);

                _logger.LogInformation("Google sign-in successful for {Email}", email);
                return new OkObjectResult(new
                {
                    id = existingUser.Id,
                    token = tokenString,
                    expiresIn = 3600,
                    refreshToken = newRefreshToken.Token,
                    kindOfWork = existingUser.KindOfWork,
                    medicalSpecialist = existingUser.MedicalSpecialist,
                    isAdmin = existingUser.IsAdmin,
                    requiresProfileCompletion = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SignInWithGoogle");
                return new ObjectResult(new { message = "An error occurred during Google sign-in: " + ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> CompleteGoogleProfileAsync(GoogleProfileCompletionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Address) ||
                    string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
                {
                    _logger.LogWarning("Missing required fields for Google profile completion: {Email}", request.Email);
                    return new BadRequestObjectResult(new { message = "Email, phone, address, password, and confirm password are required." });
                }

                if (request.Password != request.ConfirmPassword)
                {
                    _logger.LogWarning("Passwords do not match for Google profile completion: {Email}", request.Email);
                    return new BadRequestObjectResult(new { message = "Passwords do not match." });
                }

                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("User already exists: {Email}", request.Email);
                    return new BadRequestObjectResult(new { message = "User already exists." });
                }

                var (passwordHash, passwordSalt) = PasswordHasher.CreatePasswordHash(request.Password);

                var user = new User
                {
                    Email = request.Email,
                    Name = request.Name ?? "Google User",
                    Phone = request.Phone,
                    Address = request.Address,
                    MedicalSpecialist = request.MedicalSpecialist,
                    KindOfWork = "Doctor",
                    IsAdmin = false,
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    ProfileImage = "",
                    Status = UserStatus.Deactivated
                };

                _context.users.Add(user);
                await _context.SaveChangesAsync();

                var (tokenString, newRefreshToken) = await GenerateTokensAsync(user);

                _logger.LogInformation("Google profile completed for {Email}", request.Email);
                return new OkObjectResult(new
                {
                    message = "Profile completed successfully",
                    id = user.Id,
                    token = tokenString,
                    refreshToken = newRefreshToken.Token,
                    kindOfWork = user.KindOfWork,
                    medicalSpecialist = user.MedicalSpecialist,
                    isAdmin = user.IsAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteGoogleProfile for {Email}", request.Email);
                return new ObjectResult(new { message = "An error occurred while completing profile: " + ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var existingToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (existingToken == null || existingToken.ExpiryDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired refresh token");
                    return new UnauthorizedObjectResult(new { message = "Invalid or expired refresh token." });
                }

                var (tokenString, newRefreshToken) = await GenerateTokensAsync(existingToken.User);

                _logger.LogInformation("Token refreshed for UserId: {UserId}", existingToken.User.Id);
                return new OkObjectResult(new
                {
                    id = existingToken.User.Id,
                    token = tokenString,
                    expiresIn = 3600,
                    refreshToken = newRefreshToken.Token,
                    kindOfWork = existingToken.User.KindOfWork,
                    medicalSpecialist = existingToken.User.MedicalSpecialist,
                    isAdmin = existingToken.User.IsAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RefreshToken");
                return new ObjectResult(new { message = "An error occurred during token refresh." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> LogoutAsync(string refreshToken)
        {
            try
            {
                var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
                if (existingToken != null)
                {
                    _context.RefreshTokens.Remove(existingToken);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("User logged out");
                return new OkObjectResult(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Logout");
                return new ObjectResult(new { message = "An error occurred during logout." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> AddProfileImageAsync(string email, AddProfileImagecsDto imageDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Email is required for profile image upload");
                    return new BadRequestObjectResult(new { message = "Email is required." });
                }

                var user = await _context.users.FirstOrDefaultAsync(c => c.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("Invalid user email: {Email}", email);
                    return new BadRequestObjectResult(new { message = "Invalid user email." });
                }

                if (imageDto.ProfileImage == null)
                {
                    _logger.LogWarning("Profile image is required for {Email}", email);
                    return new BadRequestObjectResult(new { message = "Profile image is required." });
                }

                var ext = Path.GetExtension(imageDto.ProfileImage.FileName).ToLower();
                if (!_allowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("Unsupported image format: {Extension} for {Email}", ext, email);
                    return new BadRequestObjectResult(new { message = "Unsupported image format. Allowed formats: jpg, jpeg, png, gif, bmp, webp, tiff, tif, svg, ico, heif." });
                }

                if (imageDto.ProfileImage.Length > _maxAllowedImageSize)
                {
                    _logger.LogWarning("Image size {Size} exceeds maximum allowed size {MaxSize} for {Email}", imageDto.ProfileImage.Length, _maxAllowedImageSize, email);
                    return new BadRequestObjectResult(new { message = "Image size exceeds 10 MB." });
                }

                using var stream = imageDto.ProfileImage.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(imageDto.ProfileImage.FileName, stream),
                    PublicId = $"users/{Guid.NewGuid()}",
                    Folder = "profile_images",
                    Transformation = new Transformation().Width(150).Height(150).Crop("fill")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                {
                    _logger.LogError("Cloudinary upload error: {Error} for {Email}", uploadResult.Error.Message, email);
                    return new ObjectResult(new { message = "Failed to upload image to Cloudinary." }) { StatusCode = 500 };
                }

                user.ProfileImage = uploadResult.SecureUrl?.ToString() ?? string.Empty;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile image uploaded for {Email}", email);
                return new OkObjectResult(new
                {
                    message = "Profile image uploaded successfully.",
                    imageUrl = user.ProfileImage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddProfileImage for {Email}", email);
                return new ObjectResult(new { message = "An error occurred while uploading the profile image." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> GetUserAsync(string email)
        {
            try
            {
                var existingUser = await _context.users
                    .Include(u => u.Products)
                    .Include(u => u.ContactUs)
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser == null)
                {
                    _logger.LogWarning("User not found for {Email}", email);
                    return new NotFoundObjectResult(new { message = "User not found." });
                }

                _logger.LogInformation("Retrieved user {Email}", existingUser.Email);
                return new OkObjectResult(new
                {
                    id = existingUser.Id,
                    existingUser.Name,
                    existingUser.Email,
                    existingUser.Phone,
                    existingUser.MedicalSpecialist,
                    existingUser.Address,
                    existingUser.ProfileImage,
                    existingUser.CreatedAt,
                    existingUser.KindOfWork,
                    existingUser.IsAdmin,
                    existingUser.Status,
                    isBuyer = existingUser.isBuyer,
                    Products = existingUser.Products.Select(p => new
                    {
                        p.UserId,
                        p.Name,
                        p.Description,
                        p.Price,
                        p.ImageUrls
                    }).ToList(),
                    ContactUsMessages = existingUser.ContactUs.Select(c => new
                    {
                        c.UserId,
                        c.Message,
                        c.CreatedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUser for {Email}", email);
                return new ObjectResult(new { message = "An error occurred while retrieving the user." }) { StatusCode = 500 };
            }
        }
        public async Task<IActionResult> UpdateAsync(string email, [FromForm] UpdateUserForm dto)
        {
            try
            {
                if (email.ToLower() != dto.Email.ToLower())
                {
                    _logger.LogWarning("Email mismatch: {ProvidedEmail} does not match {DtoEmail}", email, dto.Email);
                    return new BadRequestObjectResult(new { message = "Email mismatch." });
                }

                _logger.LogInformation("Searching for user with email: {Email}", email);
                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
                if (existingUser == null)
                {
                    _logger.LogWarning("No user found for email: {Email}", email);
                    return new NotFoundObjectResult(new { message = "User not found." });
                }

                existingUser.Name = dto.Name;
                existingUser.MedicalSpecialist = existingUser.KindOfWork == "Doctor" ? dto.MedicalSpecialist : null;
                existingUser.Address = dto.Address;
                existingUser.Phone = dto.Phone;

                if (!string.IsNullOrWhiteSpace(dto.ProfileImage))
                {
                    existingUser.ProfileImage = dto.ProfileImage; // Store Cloudinary URL directly
                }

                _context.users.Update(existingUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Profile updated for {Email}", email);
                return new OkObjectResult(new { message = "Profile updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUser for {Email}", email);
                return new ObjectResult(new { message = "An error occurred while updating the profile." })
                {
                    StatusCode = 500
                };
            }
        }
        public async Task<IActionResult> UpdateUserInfoAsync(string email, RoleSpecialistUpdateDto dto)
        {
            try
            {
                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser == null)
                {
                    _logger.LogWarning("User not found for {Email}", email);
                    return new NotFoundObjectResult(new { message = "User not found." });
                }

                if (!string.IsNullOrWhiteSpace(dto.KindOfWork))
                {
                    var workType = await _context.WorkType.FirstOrDefaultAsync(wt => wt.Name == dto.KindOfWork);
                    if (workType == null)
                    {
                        _logger.LogWarning("Invalid work type: {KindOfWork} for {Email}", dto.KindOfWork, email);
                        return new BadRequestObjectResult(new { message = $"Invalid work type: {dto.KindOfWork}" });
                    }
                    existingUser.KindOfWork = dto.KindOfWork;
                }

                if (existingUser.KindOfWork == "Doctor")
                {
                    if (string.IsNullOrWhiteSpace(dto.MedicalSpecialist))
                    {
                        _logger.LogWarning("Medical specialist is required for Doctor: {Email}", email);
                        return new BadRequestObjectResult(new { message = "Medical specialist is required for Doctor." });
                    }
                    var specialty = await _context.MedicalSpecialties.FirstOrDefaultAsync(ms => ms.Name == dto.MedicalSpecialist);
                    if (specialty == null)
                    {
                        _logger.LogWarning("Invalid specialty: {MedicalSpecialist} for {Email}", dto.MedicalSpecialist, email);
                        return new BadRequestObjectResult(new { message = $"Invalid specialty: {dto.MedicalSpecialist}" });
                    }
                    existingUser.MedicalSpecialist = dto.MedicalSpecialist;
                }
                else
                {
                    existingUser.MedicalSpecialist = null;
                }

                _context.users.Update(existingUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("KindOfWork and MedicalSpecialist updated for {Email}", email);
                return new OkObjectResult(new { message = "KindOfWork and MedicalSpecialist updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUserInfo for {Email}", email);
                return new ObjectResult(new { message = "An error occurred while updating user info." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> IsDeliveryAsync(int userId)
        {
            try
            {
                var isDelivery = await _context.DeliveryPersons.AnyAsync(d => d.UserId == userId && d.RequestStatus == "Approved");
                if (!isDelivery)
                {
                    _logger.LogWarning("Delivery person not found for UserId: {UserId}", userId);
                    return new NotFoundObjectResult(new { message = "Delivery not found." });
                }

                _logger.LogInformation("User {UserId} is a delivery person", userId);
                return new OkObjectResult(new { message = "User is a delivery person." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsDelivery for UserId: {UserId}", userId);
                return new ObjectResult(new { message = "Server error", error = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User not found for ID: {Id}", id);
                    return new NotFoundObjectResult(new { message = "User not found." });
                }

                _context.users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User deleted: ID {Id}", id);
                return new OkObjectResult(new { message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteUser for ID: {Id}", id);
                return new ObjectResult(new { message = "An error occurred while deleting the user." }) { StatusCode = 500 };
            }
        }

        private async Task<(string, RefreshToken)> GenerateTokensAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var newRefreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id);
            _context.RefreshTokens.RemoveRange(oldTokens);
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return (tokenString, newRefreshToken);
        }
    }
}
using MedBridge.Dtos;
using MedBridge.Dtos.AddProfileImagecsDtoUser;
using MedBridge.Models;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using MoviesApi.models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [ApiController]
    [Route("api/MedBridge")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;
        private readonly IMemoryCache _memoryCache;

        private readonly string _imageUploadPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "images");
        private readonly string _baseUrl = "https://10.0.2.2:7273";

        private readonly List<string> _allowedExtensions = new List<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif", ".svg", ".ico", ".heif"
        };

        private readonly double _maxAllowedImageSize = 10 * 1024 * 1024;

        public UserController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<UserController> logger,
            IMemoryCache memoryCache)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        private async Task<bool> IsUserAdmin(string email)
        {
            var user = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null && user.IsAdmin;
        }

        [HttpPost("User/signup")]
        public async Task<IActionResult> SignUp([FromForm] SignUpDto signUpDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(signUpDto.Name) || string.IsNullOrWhiteSpace(signUpDto.Email) ||
                    string.IsNullOrWhiteSpace(signUpDto.Password) || string.IsNullOrWhiteSpace(signUpDto.ConfirmPassword))
                {
                    return BadRequest(new { message = "Name, email, password, and confirm password are required." });
                }

                if (signUpDto.Password != signUpDto.ConfirmPassword)
                {
                    return BadRequest(new { message = "Passwords do not match." });
                }

                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == signUpDto.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email already exists." });
                }

                var (passwordHash, passwordSalt) = PasswordHasher.CreatePasswordHash(signUpDto.Password);

                var user = new User
                {
                    Name = signUpDto.Name,
                    Email = signUpDto.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Phone = signUpDto.Phone,
                    Address = signUpDto.Address,
                    CreatedAt = DateTime.UtcNow,
                    KindOfWork = "Doctor",
                    MedicalSpecialist = null,
                    ProfileImage = "",
                    IsAdmin = signUpDto.IsAdmin
                };

                _context.users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User registered: {Email}", user.Email);
                return Ok(new
                {
                    message = "User registered successfully!",
                    id = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SignUp for {Email}", signUpDto.Email);
                return StatusCode(500, new { message = "An error occurred during signup." });
            }
        }

    
    
        [HttpPost("User/signin")]
        public async Task<IActionResult> SignIn([FromForm] SignInDto loginRequest)
        {
            try
            {
                var cacheKey = $"LoginAttempts_{loginRequest.Email}";
                var attempts = _memoryCache.Get<int>(cacheKey);

                if (attempts >= 5)
                {
                    return BadRequest(new { message = "Too many failed attempts. Try again after 15 minutes." });
                }

                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);
                if (existingUser == null)
                {
                    _memoryCache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(15));
                    return BadRequest(new { message = "Invalid email or password." });
                }

                bool isPasswordValid = PasswordHasher.VerifyPasswordHash(
                    loginRequest.Password,
                    existingUser.PasswordHash,
                    existingUser.PasswordSalt);

                if (!isPasswordValid)
                {
                    _memoryCache.Set(cacheKey, attempts + 1, TimeSpan.FromMinutes(15));
                    return BadRequest(new { message = "Invalid email or password." });
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, existingUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, existingUser.Name),
                    new Claim(ClaimTypes.Email, existingUser.Email),
                    new Claim("IsAdmin", existingUser.IsAdmin.ToString())
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
                    UserId = existingUser.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(7)
                };

                var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == existingUser.Id);
                _context.RefreshTokens.RemoveRange(oldTokens);
                _context.RefreshTokens.Add(newRefreshToken);
                await _context.SaveChangesAsync();

                _memoryCache.Remove(cacheKey);
                return Ok(new
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
                _logger.LogError(ex, "Error in SignIn for {Email}", loginRequest.Email);
                return StatusCode(500, new { message = "An error occurred during signin." });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromForm] string refreshToken)
        {
            try
            {
                var existingToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

                if (existingToken == null || existingToken.ExpiryDate <= DateTime.UtcNow)
                {
                    return Unauthorized(new { message = "Invalid or expired refresh token." });
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, existingToken.User.Id.ToString()),
                    new Claim(ClaimTypes.Name, existingToken.User.Name),
                    new Claim(ClaimTypes.Email, existingToken.User.Email),
                    new Claim("IsAdmin", existingToken.User.IsAdmin.ToString())
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var newJwtToken = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: credentials
                );

                var newTokenString = new JwtSecurityTokenHandler().WriteToken(newJwtToken);

                existingToken.Token = Guid.NewGuid().ToString();
                existingToken.ExpiryDate = DateTime.UtcNow.AddDays(7);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    id = existingToken.User.Id,
                    token = newTokenString,
                    expiresIn = 3600,
                    refreshToken = existingToken.Token,
                    kindOfWork = existingToken.User.KindOfWork,
                    medicalSpecialist = existingToken.User.MedicalSpecialist,
                    isAdmin = existingToken.User.IsAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RefreshToken");
                return StatusCode(500, new { message = "An error occurred during token refresh." });
            }
        }

        [HttpPost("User/logout")]
        public async Task<IActionResult> Logout([FromForm] string refreshToken)
        {
            try
            {
                var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
                if (existingToken != null)
                {
                    _context.RefreshTokens.Remove(existingToken);
                    await _context.SaveChangesAsync();
                }
                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Logout");
                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        [HttpPost("User/addProfileImage")]
        public async Task<IActionResult> AddProfileImage(string email, [FromForm] AddProfileImagecsDto imageDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new { message = "Email is required." });
                }

                var user = await _context.users.FirstOrDefaultAsync(c => c.Email == email);
                if (user == null)
                {
                    return BadRequest(new { message = "Invalid user email." });
                }

                if (imageDto.ProfileImage == null)
                {
                    return BadRequest(new { message = "Profile image is required." });
                }

                var ext = Path.GetExtension(imageDto.ProfileImage.FileName).ToLower();
                if (!_allowedExtensions.Contains(ext))
                {
                    return BadRequest(new { message = "Unsupported image format." });
                }

                if (imageDto.ProfileImage.Length > _maxAllowedImageSize)
                {
                    return BadRequest(new { message = "Image size exceeds 10 MB." });
                }

                var fileName = Guid.NewGuid() + ext;
                var savePath = Path.Combine(_imageUploadPath, fileName);

                Directory.CreateDirectory(_imageUploadPath);
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await imageDto.ProfileImage.CopyToAsync(stream);
                }

                user.ProfileImage = $"{_baseUrl}/images/{fileName}";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Profile image uploaded successfully.", imageUrl = user.ProfileImage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddProfileImage for {Email}", email);
                return StatusCode(500, new { message = "An error occurred while uploading the profile image." });
            }
        }

        [HttpGet("User/{email}")]
        public async Task<IActionResult> GetUser(string email)
        {
            try
            {
                var existingUser = await _context.users
                    .Include(u => u.Products)
                    .Include(u => u.ContactUs)
                    .FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser == null)
                {
                    return NotFound(new { message = "User not found." });
                }
                return Ok(new
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
                        c.created
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUser for {Email}", email);
                return StatusCode(500, new { message = "An error occurred while retrieving the user." });
            }
        }

        [HttpPut("User/{email}")]
        public async Task<IActionResult> UpdateUser(string email, [FromForm] UpdateUserDto updatedUser, IFormFile? profileImage)
        {
            try
            {
                if (email != updatedUser.Email)
                {
                    return BadRequest(new { message = "Email mismatch." });
                }

                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                existingUser.Name = updatedUser.Name;
                existingUser.MedicalSpecialist = existingUser.KindOfWork == "Doctor" ? updatedUser.MedicalSpecialist : null;
                existingUser.Address = updatedUser.Address;

                if (!string.IsNullOrWhiteSpace(updatedUser.Phone))
                {
                    existingUser.Phone = updatedUser.Phone;
                }

                if (profileImage != null)
                {
                    var ext = Path.GetExtension(profileImage.FileName).ToLower();
                    if (!_allowedExtensions.Contains(ext))
                    {
                        return BadRequest(new { message = "Unsupported image format." });
                    }

                    if (profileImage.Length > _maxAllowedImageSize)
                    {
                        return BadRequest(new { message = "Image size exceeds 10 MB." });
                    }

                    var fileName = Guid.NewGuid() + ext;
                    var savePath = Path.Combine(_imageUploadPath, fileName);

                    Directory.CreateDirectory(_imageUploadPath);
                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(stream);
                    }

                    existingUser.ProfileImage = $"{_baseUrl}/images/{fileName}";
                }

                _context.users.Update(existingUser);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Profile updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUser for {Email}", email);
                return StatusCode(500, new { message = "An error occurred while updating the profile." });
            }
        }

        [HttpPatch("User/info/{email}")]
        public async Task<IActionResult> UpdateUserInfo(string email, [FromBody] RoleSpecialistUpdateDto dto)
        {
            try
            {
                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                if (!string.IsNullOrWhiteSpace(dto.KindOfWork))
                {
                    var workType = await _context.WorkType.FirstOrDefaultAsync(wt => wt.Name == dto.KindOfWork);
                    if (workType == null)
                    {
                        return BadRequest(new { message = $"Invalid work type: {dto.KindOfWork}" });
                    }
                    existingUser.KindOfWork = dto.KindOfWork;
                }

                if (existingUser.KindOfWork == "Doctor")
                {
                    if (string.IsNullOrWhiteSpace(dto.MedicalSpecialist))
                    {
                        return BadRequest(new { message = "Medical specialist is required for Doctor." });
                    }
                    var specialty = await _context.MedicalSpecialties.FirstOrDefaultAsync(ms => ms.Name == dto.MedicalSpecialist);
                    if (specialty == null)
                    {
                        return BadRequest(new { message = " $Invalid specialty: { dto.MedicalSpecialist}" });
                    }
                    existingUser.MedicalSpecialist = dto.MedicalSpecialist;
                }
                else
                {
                    existingUser.MedicalSpecialist = null;
                }

                _context.users.Update(existingUser);
                await _context.SaveChangesAsync();
                return Ok(new { message = "KindOfWork and MedicalSpecialist updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUserInfo for {Email}", email);
                return StatusCode(500, new { message = "An error occurred while updating user info." });
            }
        }

        [HttpDelete("User/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }

                _context.users.Remove(user);
                await _context.SaveChangesAsync();
                return Ok(new { message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteUser for ID {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the user." });
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "Server is online" });
        }
    }


}
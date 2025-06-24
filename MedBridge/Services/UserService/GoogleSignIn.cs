using Google.Apis.Auth;
using MedBridge.Models;
using MedBridge.Models.GoogLe_signIn;
using Microsoft.IdentityModel.Tokens;
using MoviesApi.models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MedBridge.Services.UserService
{
    public class GoogleSignIn : IGoogleSignIn
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public GoogleSignIn(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<GoogleLoginResponse> SignInWithGoogle(string googleToken)
        {
            try
            {
                // Validate Google ID token
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleToken);
                var email = payload.Email;

                var user = _context.users.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    // New user
                    return new GoogleLoginResponse
                    {
                        Status = "new_user",
                        Email = email
                    };
                }

                // Existing user - generate JWT token
                var token = GenerateJwtToken(user);
                return new GoogleLoginResponse
                {
                    Status = "existing_user",
                    Email = user.Email,
                    Token = token,
                    Id = user.Id,
                    KindOfWork = user.KindOfWork,
                    MedicalSpecialist = user.MedicalSpecialist,
                    IsAdmin = user.IsAdmin
                };
            }
            catch (InvalidJwtException)
            {
                throw new Exception("Invalid Google token");
            }
        }

        public async Task<bool> CompleteProfile(UserProfileRequest request)
        {
            try
            {
                var existingUser = _context.users.FirstOrDefault(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    return false; // User already exists
                }

                var user = new User
                {
                    Email = request.Email,
                    Phone = request.Phone,
                    Address = request.Address,
                    MedicalSpecialist = request.MedicalSpecialist,
                    KindOfWork = "Doctor",
                    CreatedAt = DateTime.UtcNow,
                    IsAdmin = false
                };

                _context.users.Add(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error completing profile: {ex.Message}");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("IsAdmin", user.IsAdmin.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}
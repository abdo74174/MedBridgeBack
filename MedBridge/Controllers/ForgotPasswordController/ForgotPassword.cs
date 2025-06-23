using MedBridge.Models;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MoviesApi.models;

namespace MedBridge.Controllers
{
    [ApiController]
    [Route("api/ForgotPassword")]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<ForgotPasswordController> _logger;
        private readonly FirebaseAuth _firebaseAuth;

        public ForgotPasswordController(
            ApplicationDbContext context,
            EmailService emailService,
            ILogger<ForgotPasswordController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;

            try
            {
                if (FirebaseApp.DefaultInstance == null)
                {
                    var firebaseConfigJson = Environment.GetEnvironmentVariable("FIREBASE_CONFIG");
                    if (!string.IsNullOrEmpty(firebaseConfigJson))
                    {
                        _logger.LogInformation("Initializing Firebase from environment variable");
                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromJson(firebaseConfigJson),
                        });
                    }
                    else
                    {
                        var credentialPath = configuration["Firebase:CredentialPath"] ?? "Config/firebase-service-account.json";
                        var fullPath = Path.Combine(AppContext.BaseDirectory, credentialPath);
                        _logger.LogInformation("Initializing Firebase from file: {Path}", fullPath);

                        if (!System.IO.File.Exists(fullPath))
                        {
                            _logger.LogError("Firebase credential file not found at: {Path}", fullPath);
                            throw new FileNotFoundException($"Firebase credential file not found at: {fullPath}");
                        }

                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromFile(fullPath),
                        });
                    }
                }

                _firebaseAuth = FirebaseAuth.DefaultInstance;
                _logger.LogInformation("Firebase initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase");
                throw new Exception("Firebase authentication service initialization failed.", ex);
            }
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { success = false, message = "Validation errors", errors = errors });
                }

                var user = await _context.users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Email not found: {Email}", dto.Email);
                    return BadRequest(new { success = false, message = "Email not found." });
                }

                var otp = new Random().Next(100000, 999999).ToString();
                user.ResetToken = otp;
                user.ResetTokenExpires = DateTime.UtcNow.AddMinutes(15);

                _context.users.Update(user);
                await _context.SaveChangesAsync();

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Password Reset OTP",
                    $"Your OTP is {otp}. It expires in 15 minutes."
                );

                _logger.LogInformation("OTP sent to {Email}", user.Email);
                return Ok(new { success = true, message = "OTP sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP for {Email}", dto.Email);
                return StatusCode(500, new { success = false, message = "An error occurred while sending OTP.", error = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { success = false, message = "Validation errors", errors = errors });
                }

                var user = await _context.users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Email not found: {Email}", dto.Email);
                    return BadRequest(new { success = false, message = "Email not found." });
                }

                if (user.ResetToken != dto.Otp || user.ResetTokenExpires < DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired OTP for {Email}", dto.Email);
                    return BadRequest(new { success = false, message = "Invalid or expired OTP." });
                }

                var (passwordHash, passwordSalt) = PasswordHasher.CreatePasswordHash(dto.NewPassword);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
                user.ResetToken = null;
                user.ResetTokenExpires = null;

                _context.users.Update(user);
                await _context.SaveChangesAsync();

                string customToken;
                try
                {
                    _logger.LogInformation("Fetching Firebase user for {Email}", dto.Email);
                    var firebaseUser = await _firebaseAuth.GetUserByEmailAsync(dto.Email);
                    customToken = await _firebaseAuth.CreateCustomTokenAsync(firebaseUser.Uid);
                    _logger.LogInformation("Generated custom token for {Email}: {Token}", dto.Email, customToken);
                }
                catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
                {
                    _logger.LogInformation("Firebase user not found, creating new user for {Email}", dto.Email);
                    try
                    {
                        var firebaseUser = await _firebaseAuth.CreateUserAsync(new UserRecordArgs
                        {
                            Email = dto.Email,
                            EmailVerified = true,
                            Password = dto.NewPassword,
                            Disabled = false
                        });

                        customToken = await _firebaseAuth.CreateCustomTokenAsync(firebaseUser.Uid);
                        _logger.LogInformation("Created Firebase user and generated custom token for {Email}: {Token}", dto.Email, customToken);
                    }
                    catch (Exception createEx)
                    {
                        _logger.LogError(createEx, "Failed to create Firebase user for {Email}", dto.Email);
                        return StatusCode(500, new { success = false, message = "Failed to create Firebase user.", error = createEx.Message });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Firebase error for {Email}", dto.Email);
                    return StatusCode(500, new { success = false, message = "Firebase authentication error.", error = ex.Message });
                }

                if (string.IsNullOrEmpty(customToken))
                {
                    _logger.LogError("Custom token is null or empty for {Email}", dto.Email);
                    return StatusCode(500, new { success = false, message = "Failed to generate authentication token." });
                }

                _logger.LogInformation("Password reset successful for {Email}", dto.Email);
                var response = new
                {
                    success = true,
                    message = "Password reset successfully.",
                    customToken = customToken
                };
                _logger.LogInformation("Returning response for {Email}: {Response}", dto.Email, JsonSerializer.Serialize(response));
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for {Email}", dto.Email);
                return StatusCode(500, new { success = false, message = "An error occurred while resetting password.", error = ex.Message });
            }
        }

        public class SendOtpDto
        {
            [Required(ErrorMessage = "The email field is required.")]
            [EmailAddress(ErrorMessage = "The email address is not valid.")]
            public string Email { get; set; }
        }

        public class VerifyOtpDto
        {
            [Required]
            public string Email { get; set; }

            [Required]
            public string Otp { get; set; }

            [Required]
            [RegularExpression(
                @"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$",
                ErrorMessage = "Password must contain 8+ chars, 1 letter, 1 number, and 1 special character")]
            public string NewPassword { get; set; }
        }
    }
}
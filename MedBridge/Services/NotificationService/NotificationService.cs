

using Google.Apis.Auth.OAuth2;
using MedBridge.Models.NotificationModel;
using MedBridge.Models.UsersModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using Newtonsoft.Json;
using System.Text;

namespace MedBridge.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<NotificationService> _logger;
        private readonly string _projectId;
        private readonly string _serviceAccountJson;

        public NotificationService(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext dbContext,
            ILogger<NotificationService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _projectId = configuration["Firebase:ProjectId"] ?? throw new ArgumentNullException("Firebase:ProjectId is missing in configuration.");
            _serviceAccountJson = configuration["Firebase:ServiceAccountJson"] ?? throw new ArgumentNullException("Firebase:ServiceAccountJson is missing in configuration.");
        }

        public async Task<IActionResult> RegisterToken(TokenRequest tokenRequest)
        {
            try
            {
                if (string.IsNullOrEmpty(tokenRequest.Token))
                {
                    _logger.LogWarning("Token registration failed: Token is required");
                    return new BadRequestObjectResult("Token is required.");
                }

                if (tokenRequest.UserId == 0)
                {
                    _logger.LogWarning("Token registration failed: UserId is required");
                    return new BadRequestObjectResult("User ID is required.");
                }

                if (!await _dbContext.users.AnyAsync(u => u.Id == tokenRequest.UserId))
                {
                    _logger.LogWarning("Token registration failed: Invalid UserId {UserId}", tokenRequest.UserId);
                    return new BadRequestObjectResult("Invalid User ID.");
                }

                var existingToken = await _dbContext.DeviceTokens
                    .FirstOrDefaultAsync(dt => dt.Token == tokenRequest.Token && dt.UserId == tokenRequest.UserId);

                if (existingToken != null)
                {
                    _logger.LogInformation("Token already registered for userId: {UserId}", tokenRequest.UserId);
                    return new OkObjectResult(new { message = "Token already registered" });
                }

                var deviceToken = new Models.Testing.DeviceTokens
                {
                    Token = tokenRequest.Token,
                    UserId = tokenRequest.UserId
                };

                _dbContext.DeviceTokens.Add(deviceToken);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Token registered successfully for userId: {UserId}, Token: {Token}", tokenRequest.UserId, tokenRequest.Token.Substring(0, Math.Min(10, tokenRequest.Token.Length)) + "...");
                return new OkObjectResult(new { message = "Token registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering token for userId: {UserId}", tokenRequest.UserId);
                return new ObjectResult(new { error = $"Error registering token: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        public async Task<IActionResult> SendPushNotification(NotificationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.DeviceToken))
                {
                    _logger.LogWarning("Notification send failed: DeviceToken is required");
                    return new BadRequestObjectResult("Device token is required.");
                }

                if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Body))
                {
                    _logger.LogWarning("Notification send failed: Title and Body are required");
                    return new BadRequestObjectResult("Notification title and body are required.");
                }

                _logger.LogInformation("Sending notification to token {Token}: Title={Title}, Body={Body}",
                    request.DeviceToken.Substring(0, Math.Min(10, request.DeviceToken.Length)) + "...", request.Title, request.Body);

                var credential = GoogleCredential.FromJson(_serviceAccountJson)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                _logger.LogInformation("FCM access token obtained successfully");

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var message = new
                {
                    message = new
                    {
                        token = request.DeviceToken,
                        notification = new
                        {
                            title = request.Title,
                            body = request.Body
                        },
                        data = new
                        {
                            click_action = "FLUTTER_NOTIFICATION_CLICK"
                        }
                    }
                };

                var jsonMessage = JsonConvert.SerializeObject(message);
                var content = new StringContent(jsonMessage, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(
                    $"https://fcm.googleapis.com/v1/projects/{_projectId}/messages:send",
                    content);

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("FCM response: Status={StatusCode}, Body={ResponseBody}", response.StatusCode, responseBody);

                if (response.IsSuccessStatusCode)
                {
                    return new OkObjectResult(new { message = "Notification sent successfully" });
                }
                else
                {
                    _logger.LogWarning("Failed to send notification: {ResponseBody}", responseBody);
                    return new BadRequestObjectResult(new { error = $"Failed to send notification: {responseBody}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to token {Token}", request.DeviceToken.Substring(0, Math.Min(10, request.DeviceToken.Length)) + "...");
                return new ObjectResult(new { error = $"Error sending notification: {ex.Message}" })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
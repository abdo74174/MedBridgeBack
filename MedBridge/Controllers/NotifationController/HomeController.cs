using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using MoviesApi.models;
using MedBridge.Models.UsersModel;
using MedBridge.Models.NotificationModel;
using MedBridge.Models.Testing;

namespace MedBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _dbContext;
        private readonly string _projectId = "medbridge-11d7e";
        private readonly string _serviceAccountPath = "F:\\projects\\Project\\MedBridge\\MedBridge\\wwwroot\\jsonfile\\medbridge-11d7e-firebase-adminsdk-fbsvc-77f183ab5d.json";

        public NotificationController(IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterToken([FromBody] TokenRequest tokenRequest)
        {
            if (string.IsNullOrEmpty(tokenRequest.Token))
            {
                Console.WriteLine("Token registration failed: Token is required");
                return BadRequest("Token is required.");
            }

            if (!await _dbContext.users.AnyAsync(u => u.Id == tokenRequest.UserId))
            {
                Console.WriteLine($"Token registration failed: Invalid UserId {tokenRequest.UserId}");
                return BadRequest("Invalid User ID.");
            }

            var existingToken = await _dbContext.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.Token == tokenRequest.Token && dt.UserId == tokenRequest.UserId);

            if (existingToken != null)
            {
                Console.WriteLine($"Token already registered for userId: {tokenRequest.UserId}");
                return Ok(new { message = "Token already registered" });
            }

            var deviceToken = new DeviceTokens
            {
                Token = tokenRequest.Token,
                UserId = tokenRequest.UserId
            };

            _dbContext.DeviceTokens.Add(deviceToken);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"Token registered successfully for userId: {tokenRequest.UserId}, Token: {tokenRequest.Token.Substring(0, 10)}...");
            return Ok(new { message = "Token registered successfully" });
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendPushNotification([FromBody] NotificationRequest request)
        {
            try
            {
                Console.WriteLine($"Sending notification to token {request.DeviceToken.Substring(0, 10)}...: Title={request.Title}, Body={request.Body}");
                var credential = GoogleCredential.FromFile(_serviceAccountPath)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                Console.WriteLine("FCM access token obtained successfully");

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
                Console.WriteLine($"FCM response: Status={response.StatusCode}, Body={responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "Notification sent successfully" });
                }
                else
                {
                    Console.WriteLine($"Failed to send notification: {responseBody}");
                    return BadRequest(new { error = $"Failed to send notification: {responseBody}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = $"Error sending notification: {ex.Message}" });
            }
        }
    }

  

}
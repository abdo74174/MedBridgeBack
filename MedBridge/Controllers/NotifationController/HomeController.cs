using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;
using Google.Apis.Auth.OAuth2;
using MoviesApi.models;
using Microsoft.EntityFrameworkCore;
using Intersoft.Crosslight;

namespace MedBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _dbContext;
        private readonly string _projectId = "medbridge-11d7e"; // Replace with your Firebase Project ID
        private readonly string _serviceAccountPath = "F:\\projects\\Project\\MedBridge\\MedBridge\\wwwroot\\jsonfile\\service-account-key.json"; // Replace with path to JSON file

        public NotificationController(IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterToken([FromBody] TokenRequest tokenRequest)
        {
            if (string.IsNullOrEmpty(tokenRequest.Token))
                return BadRequest("Token is required.");

            // Check if token already exists for the user
            var existingToken = await _dbContext.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.Token == tokenRequest.Token && dt.UserId == tokenRequest.UserId);

            if (existingToken != null)
                return Ok(new { message = "Token already registered" });

            var deviceToken = new DeviceTokens
            {
                Token = tokenRequest.Token,
                UserId = tokenRequest.UserId
            };

            _dbContext.DeviceTokens.Add(deviceToken);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Token registered successfully" });
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendPushNotification([FromBody] NotificationRequest request)
        {
            try
            {
                var credential = GoogleCredential.FromFile(_serviceAccountPath)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

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

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { message = "Notification sent successfully" });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return BadRequest(new { error = $"Failed to send notification: {error}" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error sending notification: {ex.Message}" });
            }
        }
    }

    public class TokenRequest
    {
        public string Token { get; set; }
        public int UserId { get; set; }
    }

    public class NotificationRequest
    {
        public string DeviceToken { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
    }
}
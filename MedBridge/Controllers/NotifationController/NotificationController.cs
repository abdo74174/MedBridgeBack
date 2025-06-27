using MedBridge.Models.NotificationModel;
using MedBridge.Models.Testing;
using MedBridge.Models.UsersModel;
using MedBridge.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterToken([FromBody] TokenRequest tokenRequest)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _notificationService.RegisterToken(tokenRequest);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendPushNotification([FromBody] NotificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _notificationService.SendPushNotification(request);
        }
    }
}
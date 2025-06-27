using MedBridge.Models.NotificationModel;
using MedBridge.Models.Testing;
using MedBridge.Models.UsersModel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface INotificationService
    {
        Task<IActionResult> RegisterToken(TokenRequest tokenRequest);
        Task<IActionResult> SendPushNotification(NotificationRequest request);
    }
}
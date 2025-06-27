using MedBridge.Dtos.UserDtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services
{
    public interface IForgotPasswordService
    {
        Task InitializeFirebaseAsync();
        Task<IActionResult> SendOtp(SendOtpDto dto);
        Task<IActionResult> VerifyOtp(VerifyOtpDto dto);
    }
}
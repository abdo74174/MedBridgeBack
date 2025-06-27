using MedBridge.Dtos;
using MedBridge.Dtos.AddProfileImagecsDtoUser;
using MedBridge.Models;
using MedBridge.Models.GoogLe_signIn;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MedBridge.Services.UserService
{
    public interface IUserService
    {
        Task<IActionResult> SignUpAsync(SignUpDto dto);
        Task<IActionResult> SignInAsync(SignInDto dto);
        Task<IActionResult> SignInWithGoogleAsync(GoogleSignInRequest request);
        Task<IActionResult> CompleteGoogleProfileAsync(GoogleProfileCompletionRequest request);
        Task<IActionResult> RefreshTokenAsync(string refreshToken);
        Task<IActionResult> LogoutAsync(string refreshToken);
        Task<IActionResult> AddProfileImageAsync(string email, AddProfileImagecsDto imageDto);
        Task<IActionResult> GetUserAsync(string email);
        Task<IActionResult> UpdateAsync(string email, UpdateUserForm dto);
        Task<IActionResult> UpdateUserInfoAsync(string email, RoleSpecialistUpdateDto dto);
        Task<IActionResult> IsDeliveryAsync(int userId);
        Task<IActionResult> DeleteUserAsync(int id);
        Task<bool> IsUserAdminAsync(string email);
    }
}
using MedBridge.Dtos;
using MedBridge.Dtos.AddProfileImagecsDtoUser;
using MedBridge.Models.GoogLe_signIn;
using MedBridge.Services.UserService;
using Microsoft.AspNetCore.Mvc;

namespace MedBridge.Controllers
{
    [ApiController]
    [Route("api/MedBridge")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("User/signup")]
        public async Task<IActionResult> SignUp([FromForm] SignUpDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _userService.SignUpAsync(dto);
        }

        [HttpPost("User/signin")]
        public async Task<IActionResult> SignIn([FromForm] SignInDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _userService.SignInAsync(dto);
        }

        [HttpPost("signin/google")]
        public async Task<IActionResult> SignInWithGoogle([FromBody] GoogleSignInRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _userService.SignInWithGoogleAsync(request);
        }

        [HttpPost("signin/google/complete-profile")]
        public async Task<IActionResult> CompleteGoogleProfile([FromBody] GoogleProfileCompletionRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _userService.CompleteGoogleProfileAsync(request);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromForm] string refreshToken)
        {
            return await _userService.RefreshTokenAsync(refreshToken);
        }

        [HttpPost("User/logout")]
        public async Task<IActionResult> Logout([FromForm] string refreshToken)
        {
            return await _userService.LogoutAsync(refreshToken);
        }

        [HttpPost("User/addProfileImage")]
        public async Task<IActionResult> AddProfileImage(string email, [FromForm] AddProfileImagecsDto imageDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _userService.AddProfileImageAsync(email, imageDto);
        }

        [HttpGet("User/{email}")]
        public async Task<IActionResult> GetUser(string email)
        {
            return await _userService.GetUserAsync(email);
        }

        [HttpPut("User/{email}")]
        public async Task<IActionResult> UpdateUser(string email, [FromForm] UpdateUserForm form)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _userService.UpdateAsync(email, form);
        }

        [HttpPatch("User/info/{email}")]
        public async Task<IActionResult> UpdateUserInfo(string email, [FromBody] RoleSpecialistUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { Errors = errors });
            }

            return await _userService.UpdateUserInfoAsync(email, dto);
        }

        [HttpGet("IsDelvirey/{userId}")]
        public async Task<IActionResult> IsDelivery(int userId)
        {
            return await _userService.IsDeliveryAsync(userId);
        }

        [HttpDelete("User/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            return await _userService.DeleteUserAsync(id);
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { status = "Server is online" });
        }
    }
}
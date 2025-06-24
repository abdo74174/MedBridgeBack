using MedBridge.Models.GoogLe_signIn;
namespace MedBridge.Services.UserService
{
    public interface IGoogleSignIn
    {
        Task<GoogleLoginResponse> SignInWithGoogle(string googleToken);
        Task<bool> CompleteProfile(UserProfileRequest request);
    }
}
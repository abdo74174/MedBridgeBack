using Microsoft.AspNetCore.Http;

namespace MedBridge.Dtos.AddProfileImagecsDtoUser
{
    public class AddProfileImagecsDto
    {
        public IFormFile ProfileImage { get; set; }
    }
}

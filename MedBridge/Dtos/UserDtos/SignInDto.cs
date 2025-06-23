using System.ComponentModel.DataAnnotations;

public class SignInDto
{
    [Required(ErrorMessage = "The Email field is required.")]
    [EmailAddress(ErrorMessage = "The Email address is not valid.")]
    public string Email { get; set; }
    public string Password { get; set; }
}
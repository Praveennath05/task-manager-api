using System.Dynamic;

namespace TaskManager.Web.Models;
public class LoginRequest
{
    public string Email {get; set;} = string.Empty;
    public string Password {get; set;} = string.Empty;
}
public class AuthResponse
{
    public string AccessToken {get; set;} = string.Empty;
    public string RefreshToken {get; set;} = string.Empty;
}
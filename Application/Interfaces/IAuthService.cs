using Application.Wrappers;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResponse<string>> RegisterAsync(
        string fullName, 
        string email, 
        string password, 
        string role, 
        string paypalEmail,
        string? commentCountEstimate = null,
        string? contentTypes = null,
        string? youTubeAccountAge = null,
        string? companyName = null,
        string? useCase = null);
    Task<ServiceResponse<string>> LoginAsync(string email, string password);
    Task<ServiceResponse<string>> ForgotPasswordAsync(string email);
    Task<ServiceResponse<string>> ResetPasswordAsync(string email, string token, string newPassword);
    Task<ServiceResponse<string>> VerifyEmailAsync(string email, string token);
}

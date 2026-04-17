using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Common;
using Application.Interfaces;
using Application.Wrappers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Persistence.Context;
using Persistence.Models;

using Domain.Entities;
using Domain.Enums;
using Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly IBuyerRepository _buyerRepository;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager, 
        IConfiguration configuration,
        IVolunteerRepository volunteerRepository,
        IBuyerRepository buyerRepository,
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _volunteerRepository = volunteerRepository;
        _buyerRepository = buyerRepository;
        _context = context;
        _auditLogService = auditLogService;
        _emailService = emailService;
    }

    public async Task<ServiceResponse<string>> LoginAsync(string email, string password)
    {
        var sanitizedEmail = InputSanitizer.SanitizeEmail(email);
        var user = await _userManager.FindByEmailAsync(sanitizedEmail);
        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            await _auditLogService.LogAsync(
                action: "LoginFailed",
                entityType: "User",
                entityId: sanitizedEmail,
                success: false,
                errorMessage: "Invalid credentials");
            return new ServiceResponse<string>("Invalid credentials.");
        }

        if (user.IsSuspended)
        {
            return new ServiceResponse<string>("Your account is currently suspended.");
        }

        if (user.DeletedAt.HasValue)
        {
            return new ServiceResponse<string>("This account has been deleted.");
        }

        var normalizedRole = UserRoles.Normalize(user.Role);
        if (!string.Equals(user.Role, normalizedRole, StringComparison.Ordinal))
        {
            user.Role = normalizedRole;
            await _userManager.UpdateAsync(user);
        }

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JwtId = Guid.NewGuid().ToString(),
            DeviceName = "Web",
            IpAddress = null,
            UserAgent = null,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetExpiryMinutes())
        };
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user, session);
        await _auditLogService.LogAsync(
            action: "UserLoggedIn",
            entityType: "User",
            entityId: user.Id,
            success: true,
            userId: user.Id);
        return new ServiceResponse<string>(token, "Login successful.");
    }

    public async Task<ServiceResponse<string>> RegisterAsync(
        string fullName, 
        string email, 
        string password, 
        string role, 
        string paypalEmail,
        string? commentCountEstimate = null,
        string? contentTypes = null,
        string? youTubeAccountAge = null,
        string? companyName = null,
        string? useCase = null)
    {
        // Sanitize email input
        var sanitizedEmail = InputSanitizer.SanitizeEmail(email);
        var sanitizedPaypalEmail = InputSanitizer.SanitizeEmail(paypalEmail);
        
        // Validate email format
        if (!InputSanitizer.IsValidEmail(sanitizedEmail))
        {
            return new ServiceResponse<string>("Invalid email format.");
        }

        // Validate password strength (min 8 chars, one uppercase, one number)
        var passwordValidation = ValidatePasswordStrength(password);
        if (!passwordValidation.Success)
        {
            return passwordValidation;
        }

        // Validate role - accept simplified roles ("Contributor" or "DataClient") and normalize to full roles
        var normalizedRole = NormalizeSimplifiedRole(role);
        if (string.IsNullOrEmpty(normalizedRole))
        {
            return new ServiceResponse<string>("Invalid role. Role must be 'Contributor' or 'DataClient'.");
        }

        var userExists = await _userManager.FindByEmailAsync(sanitizedEmail);
        if (userExists != null)
        {
            await _auditLogService.LogAsync(
                action: "RegistrationFailed",
                entityType: "User",
                entityId: sanitizedEmail,
                success: false,
                errorMessage: "Email already in use");
            return new ServiceResponse<string>("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Email = sanitizedEmail,
            UserName = sanitizedEmail,
            FullName = fullName,
            Role = normalizedRole,
            PayPalEmail = sanitizedPaypalEmail,
            CompanyName = companyName ?? string.Empty,
            CompanyVerified = false,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                await _auditLogService.LogAsync(
                    action: "RegistrationFailed",
                    entityType: "User",
                    entityId: sanitizedEmail,
                    success: false,
                    errorMessage: errorMessage);
                return new ServiceResponse<string>(errorMessage);
            }

            // Automatically create linked record based on role
            if (string.Equals(normalizedRole, UserRoles.DataContributor, StringComparison.Ordinal))
            {
                var volunteer = new Volunteer
                {
                    UserId = user.Id,
                    Status = VolunteerStatus.Registered,
                    YouTubeAccountAge = youTubeAccountAge ?? "New",
                    ContentTypes = contentTypes ?? "N/A",
                    CommentCountEstimate = commentCountEstimate ?? "0",
                    Notes = "Automatically created on registration",
                    PayPalEmail = sanitizedPaypalEmail
                };
                await _volunteerRepository.AddAsync(volunteer);
            }
            else if (string.Equals(normalizedRole, UserRoles.DataClient, StringComparison.Ordinal))
            {
                var buyer = new Buyer
                {
                    UserId = user.Id,
                    CompanyName = companyName ?? fullName,
                    UseCase = useCase ?? "N/A",
                    Website = "N/A",
                    CompanyVerified = false
                };
                await _buyerRepository.AddAsync(buyer);
            }

            await transaction.CommitAsync();
            
            // Log successful registration
            await _auditLogService.LogAsync(
                action: "UserRegistered",
                entityType: "User",
                entityId: user.Id,
                success: true,
                userId: user.Id,
                newValues: "{\"Role\":\"" + normalizedRole + "\"}");

            // Send welcome email for volunteers
            if (string.Equals(normalizedRole, UserRoles.DataContributor, StringComparison.Ordinal))
            {
                await _emailService.SendWelcomeEmailAsync(user.Email ?? string.Empty, user.FullName);
            }

            var emailVerificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedVerificationToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailVerificationToken));
            var verificationLink = BuildFrontendLink($"/verify-email?email={Uri.EscapeDataString(user.Email ?? string.Empty)}&token={Uri.EscapeDataString(encodedVerificationToken)}");
            await _emailService.SendEmailVerificationAsync(user.Email ?? string.Empty, verificationLink);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        // Ensure the token always contains the normalized role claim.
        user.Role = normalizedRole;
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            JwtId = Guid.NewGuid().ToString(),
            DeviceName = "Web",
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetExpiryMinutes())
        };
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user, session);
        return new ServiceResponse<string>(token, "Registration successful.");
    }

    public async Task<ServiceResponse<string>> ForgotPasswordAsync(string email)
    {
        var sanitizedEmail = InputSanitizer.SanitizeEmail(email);
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var recentRequests = await _context.PasswordResetRequests.CountAsync(request =>
            request.Email == sanitizedEmail &&
            request.RequestedAt >= oneHourAgo);

        if (recentRequests >= 3)
        {
            return new ServiceResponse<string>("Password reset limit reached. Try again in an hour.");
        }

        var user = await _userManager.FindByEmailAsync(sanitizedEmail);
        if (user == null)
        {
            return new ServiceResponse<string>("If the account exists, a reset link has been generated.", "Password reset requested.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var resetRequest = new PasswordResetRequest
        {
            Id = Guid.NewGuid(),
            Email = sanitizedEmail,
            Token = encodedToken,
            RequestedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _context.PasswordResetRequests.Add(resetRequest);
        await _context.SaveChangesAsync();

        var resetLink = BuildFrontendLink($"/reset-password?email={Uri.EscapeDataString(sanitizedEmail)}&token={Uri.EscapeDataString(encodedToken)}");
        await _emailService.SendPasswordResetAsync(sanitizedEmail, resetLink);
        await _auditLogService.LogAsync("PasswordResetRequested", "User", user.Id, true, user.Id);
        return new ServiceResponse<string>("If the account exists, a reset link has been generated.", "Password reset requested.");
    }

    public async Task<ServiceResponse<string>> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var sanitizedEmail = InputSanitizer.SanitizeEmail(email);
        var user = await _userManager.FindByEmailAsync(sanitizedEmail);
        if (user == null)
        {
            return new ServiceResponse<string>("Invalid password reset request.");
        }

        var resetRequest = await _context.PasswordResetRequests
            .OrderByDescending(request => request.RequestedAt)
            .FirstOrDefaultAsync(request => request.Email == sanitizedEmail && request.Token == token && request.UsedAt == null && request.ExpiresAt > DateTime.UtcNow);

        if (resetRequest == null)
        {
            return new ServiceResponse<string>("Password reset link is invalid or expired.");
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);
        if (!result.Succeeded)
        {
            return new ServiceResponse<string>(string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        resetRequest.UsedAt = DateTime.UtcNow;
        user.LastPasswordChangedAt = DateTime.UtcNow;
        user.SecurityStamp = Guid.NewGuid().ToString();
        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync("PasswordResetCompleted", "User", user.Id, true, user.Id);
        return new ServiceResponse<string>("Password updated successfully.", "Password reset complete.");
    }

    public async Task<ServiceResponse<string>> VerifyEmailAsync(string email, string token)
    {
        var sanitizedEmail = InputSanitizer.SanitizeEmail(email);
        var user = await _userManager.FindByEmailAsync(sanitizedEmail);
        if (user == null)
        {
            return new ServiceResponse<string>("Invalid verification request.");
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            return new ServiceResponse<string>(string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        await _auditLogService.LogAsync("EmailVerified", "User", user.Id, true, user.Id);
        return new ServiceResponse<string>("Email verified successfully.", "Email verified successfully.");
    }

    private string GenerateJwtToken(ApplicationUser user, UserSession session)
    {
        var normalizedRole = UserRoles.Normalize(user.Role);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, session.JwtId),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Role, normalizedRole),
            new Claim("FullName", user.FullName),
            new Claim("email", user.Email!),
            new Claim("session_id", session.Id.ToString()),
            new Claim("security_stamp", user.SecurityStamp ?? string.Empty),
            new Claim("email_confirmed", user.EmailConfirmed.ToString().ToLowerInvariant()),
            new Claim("company_verified", user.CompanyVerified.ToString().ToLowerInvariant())
        };

        var secretKey = _configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var expiryMinutes = GetExpiryMinutes();
        var expires = DateTime.Now.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            _configuration["JwtSettings:Issuer"],
            _configuration["JwtSettings:Audience"],
            claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetExpiryMinutes()
    {
        var expiryMinutes = 60;
        var expiryConfig = _configuration["JwtSettings:ExpiryInMinutes"];
        if (!string.IsNullOrEmpty(expiryConfig) && double.TryParse(expiryConfig, out var parsedMinutes))
        {
            expiryMinutes = (int)parsedMinutes;
        }

        return expiryMinutes;
    }

    private string BuildFrontendLink(string relativePath)
    {
        var baseUrl = _configuration["NadenaSettings:FrontendUrl"] ?? "http://localhost:44391";
        return $"{baseUrl.TrimEnd('/')}{relativePath}";
    }

    private ServiceResponse<string> ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new ServiceResponse<string>("Password is required.");
        }

        if (password.Length < 8)
        {
            return new ServiceResponse<string>("Password must be at least 8 characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            return new ServiceResponse<string>("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsNumber))
        {
            return new ServiceResponse<string>("Password must contain at least one number.");
        }

        return new ServiceResponse<string>(string.Empty) { Success = true };
    }

    private string? NormalizeSimplifiedRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        // Accept simplified roles
        if (role.Equals("Contributor", StringComparison.OrdinalIgnoreCase))
        {
            return UserRoles.DataContributor;
        }

        if (role.Equals("DataClient", StringComparison.OrdinalIgnoreCase))
        {
            return UserRoles.DataClient;
        }

        // Also accept full role names
        return UserRoles.Normalize(role);
    }
}

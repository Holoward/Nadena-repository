using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Application.Wrappers;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;
using Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Extensions;

public static class IdentityServiceExtensions
{
    public static void AddIdentityLayer(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Validate JWT secret key is configured
        var secretKey = configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured. Please set JwtSettings:SecretKey in configuration.");
        }

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(o =>
        {
            // Only require HTTPS metadata in production
            o.RequireHttpsMetadata = !environment.IsDevelopment();
            o.SaveToken = false;
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = configuration["JwtSettings:Issuer"],
                ValidAudience = configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
            };
            o.Events = new JwtBearerEvents()
            {
                OnTokenValidated = async context =>
                {
                    var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var sessionId = context.Principal?.FindFirst("session_id")?.Value;
                    var securityStamp = context.Principal?.FindFirst("security_stamp")?.Value;

                    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
                    {
                        context.Fail("Invalid session.");
                        return;
                    }

                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId);
                    if (user == null || user.IsSuspended || user.DeletedAt.HasValue)
                    {
                        context.Fail("User is inactive.");
                        return;
                    }

                    if (!string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal))
                    {
                        context.Fail("Session expired.");
                        return;
                    }

                    if (!Guid.TryParse(sessionId, out var parsedSessionId))
                    {
                        context.Fail("Invalid session.");
                        return;
                    }

                    var session = await dbContext.UserSessions.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == parsedSessionId && item.UserId == userId);

                    if (session == null || !session.IsActive || session.RevokedAt.HasValue || session.ExpiresAt <= DateTime.UtcNow)
                    {
                        context.Fail("Session expired.");
                    }
                },
                OnAuthenticationFailed = c =>
                {
                    c.NoResult();
                    c.Response.StatusCode = 401;
                    c.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new ServiceResponse<string>("You are not authorized"));
                    return c.Response.WriteAsync(result);
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new ServiceResponse<string>("You are not Authorized"));
                    return context.Response.WriteAsync(result);
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new ServiceResponse<string>("You are not authorized to access this resource"));
                    return context.Response.WriteAsync(result);
                }
            };
        });
    }
}

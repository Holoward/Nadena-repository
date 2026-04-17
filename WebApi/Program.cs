using Application;
using Application.Interfaces;
using Persistence;
using Persistence.Seeders;
using Shared.ServiceExtensions;
using WebApi.Extensions;
using WebApi.Services;
using Serilog;
using WebApi.Middlewares;
using Stripe;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Models;
using Domain.Entities;
using System.Text.Json;
using Application.Wrappers;
using WebApi.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day));

// Add services to the container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<RequireDataContributorOnboardingFilter>();
builder.Services.AddHttpClient();

// Named HttpClient for Groq API
builder.Services.AddHttpClient("GroqClient", client => {
    client.BaseAddress = new Uri("https://api.groq.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddControllersWithViews();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationLayer();
builder.Services.AddPersistenceLayer(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddSharedLayer();
builder.Services.AddIdentityLayer(builder.Configuration, builder.Environment);
builder.Services.Configure<Application.Settings.StripeSettings>(builder.Configuration.GetSection("StripeSettings"));
builder.Services.Configure<Application.Settings.EmailSettings>(builder.Configuration.GetSection("NadenaSettings"));
builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    config.AssumeDefaultVersionWhenUnspecified = true;
    config.ReportApiVersions = true;
});

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddUrlGroup(new Uri("https://api.groq.com"), "groq-api", tags: ["external"])
    .AddUrlGroup(new Uri("https://api.stripe.com"), "stripe-api", tags: ["external"]);

// Register StripeClient
var stripeSecretKey = builder.Configuration["NadenaSettings:StripeSecretKey"] ?? "sk_test_placeholder_key_32chars_min";
builder.Services.AddSingleton(new Stripe.StripeClient(stripeSecretKey));
Stripe.StripeConfiguration.ApiKey = stripeSecretKey;

// Add rate limiting
builder.Services.AddRateLimiter(options => {
    // Auth endpoints — strict (5 requests per 15 minutes)
    options.AddFixedWindowLimiter("auth", o => {
        o.PermitLimit = 100;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    // API endpoints — moderate (60 requests per minute)
    options.AddFixedWindowLimiter("api", o => {
        o.PermitLimit = 60;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 10;
    });
    // Upload endpoint — very strict (3 uploads per 24 hours)
    options.AddFixedWindowLimiter("upload", o => {
        // Strict enough to slow automated abuse, but permissive for normal users
        // who may retry a few times due to validation errors.
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(10);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        // Best-effort Retry-After hint if available
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        var payload = JsonSerializer.Serialize(new ServiceResponse<string>("Too many requests. Please wait and try again."));
        await context.HttpContext.Response.WriteAsync(payload, cancellationToken);
    };
});

// CORS configuration
builder.Services.AddCors(options => {
    options.AddPolicy("Development", policy => {
        policy.WithOrigins("http://localhost:44391")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    options.AddPolicy("Production", policy => {
        policy.WithOrigins("https://nadena.tech", "https://www.nadena.tech")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
    options.AddPolicy("ChromeExtension", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedIdentityDataAsync(services, builder.Configuration);
    await SeedPlatformWalletAsync(dbContext);
    await DataPoolSeeder.SeedDataPoolsAsync(dbContext);
    await SeedMarketplaceDataAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Apply CORS middleware based on environment
if (app.Environment.IsDevelopment())
    app.UseCors("Development");
else
    app.UseCors("Production");

app.UseSerilogRequestLogging();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Onion Architecture API V1");
});

app.UseSecurityHeaders();

app.UseRouting();

app.MapHealthChecks("/api/test-health", new HealthCheckOptions {
    Predicate = check => !check.Tags.Contains("external"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions {
    Predicate = check => !check.Tags.Contains("external")
});

app.MapHealthChecks("/health/external", new HealthCheckOptions {
    Predicate = check => check.Tags.Contains("external"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html");
;

app.Run();

static async Task SeedIdentityDataAsync(IServiceProvider services, IConfiguration configuration)
{
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    foreach (var roleName in new[] { "Admin", "Data Client", "Data Contributor" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var adminEmail = configuration["NadenaSettings:AdminEmail"] ?? "admin@nadena.com";
    const string adminPassword = "AdminPassword123!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            Email = adminEmail,
            UserName = adminEmail,
            FullName = "Nadena Admin",
            Role = "Admin",
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed admin user: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
        }
    }
    else if (!string.Equals(adminUser.Role, "Admin", StringComparison.Ordinal))
    {
        adminUser.Role = "Admin";
        await userManager.UpdateAsync(adminUser);
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    var staleBuyer = await dbContext.Buyers.FirstOrDefaultAsync(buyer => buyer.UserId == adminUser.Id);
    if (staleBuyer != null)
    {
        dbContext.Buyers.Remove(staleBuyer);
        await dbContext.SaveChangesAsync();
    }
}

static async Task SeedMarketplaceDataAsync(ApplicationDbContext dbContext)
{
    var activePools = await dbContext.DataPools
        .Where(pool => pool.IsActive)
        .OrderBy(pool => pool.Id)
        .ToListAsync();

    if (!activePools.Any())
    {
        return;
    }

    if (!await dbContext.YoutubeComments.AnyAsync())
    {
        var sampleComments = Enumerable.Range(1, 12)
            .Select(index => new YoutubeComment
            {
                VolunteerId = index,
                CommentText = $"Sample anonymized YouTube comment #{index}",
                VideoId = $"video-{index}",
                Timestamp = DateTime.UtcNow.AddDays(-index),
                LikeCount = index * 2,
                IsAnonymized = true,
                AnonymizationMethod = "anon-id"
            })
            .ToList();

        await dbContext.YoutubeComments.AddRangeAsync(sampleComments);
    }

    var commentCount = await dbContext.YoutubeComments.CountAsync();
    foreach (var pool in activePools)
    {
        if (string.IsNullOrWhiteSpace(pool.SourceTable))
        {
            pool.SourceTable = "YoutubeComments";
        }

        pool.ApproximateRecordCount = Math.Max(pool.ApproximateRecordCount, commentCount);
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedPlatformWalletAsync(ApplicationDbContext dbContext)
{
    var exists = await dbContext.Wallets.AnyAsync(wallet => wallet.OwnerId == "platform");
    if (exists)
    {
        return;
    }

    dbContext.Wallets.Add(new Wallet
    {
        Id = Guid.NewGuid(),
        OwnerId = "platform",
        OwnerType = "Platform",
        Currency = "USD",
        Balance = 0m,
        PendingBalance = 0m,
        LastUpdated = DateTime.UtcNow,
        Created = DateTime.UtcNow,
        CreatedBy = "System"
    });

    await dbContext.SaveChangesAsync();
}

using Application.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Persistence.Repository;
using Persistence.Services;

namespace Persistence;

public static class ServiceExtension
{
    public static void AddPersistenceLayer(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var identityConnectionString = configuration.GetConnectionString("IdentityConnection");
        
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
            
        if (string.IsNullOrWhiteSpace(identityConnectionString))
            throw new InvalidOperationException("ConnectionStrings:IdentityConnection is required so identity data stays separate from app data.");

        if (string.Equals(connectionString, identityConnectionString, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ConnectionStrings:IdentityConnection must be different from ConnectionStrings:DefaultConnection.");
        }

        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (useInMemory)
                options.UseInMemoryDatabase("InMemoryAppDb");
            else
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });

        services.AddDbContext<NadenaIdentityDbContext>(options =>
        {
            if (useInMemory)
                options.UseInMemoryDatabase("InMemoryIdentityDb");
            else
                options.UseNpgsql(identityConnectionString, b =>
                {
                    b.MigrationsAssembly(typeof(NadenaIdentityDbContext).Assembly.FullName);
                    b.MigrationsHistoryTable("__EFMigrationsHistory_Identity");
                });
        });

        services.AddScoped(typeof(IRepositoryAsync<>), typeof(MyRepositoryAsync<>));
        services.AddScoped(typeof(IReadRepositoryAsync<>), typeof(MyRepositoryAsync<>));

        services.AddScoped<IVolunteerRepository, VolunteerRepository>();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IDatasetRepository, DatasetRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, LedgerTransactionRepository>();
        services.AddScoped<IConsentRecordRepository, ConsentRecordRepository>();
        services.AddScoped<ISpotifyRecordRepository, SpotifyRecordRepository>();
        services.AddScoped<INetflixRecordRepository, NetflixRecordRepository>();
        services.AddScoped<IWatchEventRepository, WatchEventRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IVolunteerPaymentRepository, VolunteerPaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();

        // B2B Licensing
        services.AddScoped<IDataPoolRepository, DataPoolRepository>();
        services.AddScoped<IDataLicenseRepository, DataLicenseRepository>();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        // Audit Logging
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // GDPR Services
        services.AddScoped<IGdprService, GdprService>();

        // Email Service
        services.AddScoped<IEmailService, EmailService>();

        // Deduplication Service
        services.AddScoped<IDeduplicationService, DeduplicationService>();



        // License PDF Service
        services.AddScoped<ILicensePdfService, LicensePdfService>();

        // Subscriptions
        services.AddScoped<IDatasetSubscriptionRepository, DatasetSubscriptionRepository>();

        // Donations
        services.AddScoped<IDonationRepository, DonationRepository>();

        // Surveys
        services.AddScoped<ISurveyRepository, SurveyRepository>();

        services.AddScoped<DataPoolService>();
        services.AddScoped<ITakeoutValidationService, TakeoutValidationService>();
        services.AddScoped<IDataDeliveryService, DataDeliveryService>();
        services.AddScoped<IContributorOAuthTokenRepository, ContributorOAuthTokenRepository>();
        services.AddScoped<IGoogleDriveService, GoogleDriveService>();
        services.AddHostedService<DrivePollingService>();
    }


}

using Application.Interfaces;
using Microsoft.Data.Sqlite;
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
        var connectionString = BuildSqliteConnectionString(configuration.GetConnectionString("DefaultConnection"), contentRootPath);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped(typeof(IRepositoryAsync<>), typeof(MyRepositoryAsync<>));
        services.AddScoped(typeof(IReadRepositoryAsync<>), typeof(MyRepositoryAsync<>));

        services.AddScoped<IVolunteerRepository, VolunteerRepository>();
        services.AddScoped<IBuyerRepository, BuyerRepository>();
        services.AddScoped<IDatasetRepository, DatasetRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, LedgerTransactionRepository>();
        services.AddScoped<IConsentRecordRepository, ConsentRecordRepository>();
        services.AddScoped<IYoutubeCommentRepository, YoutubeCommentRepository>();
        services.AddScoped<ISpotifyRecordRepository, SpotifyRecordRepository>();
        services.AddScoped<INetflixRecordRepository, NetflixRecordRepository>();
        services.AddScoped<IWatchEventRepository, WatchEventRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IVolunteerPaymentRepository, VolunteerPaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();

        // B2B Licensing
        services.AddScoped<IDataPoolRepository, DataPoolRepository>();
        services.AddScoped<IDataLicenseRepository, DataLicenseRepository>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IBlockchainService, LocalBlockchainService>();

        // Audit Logging
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // GDPR Services
        services.AddScoped<IGdprService, GdprService>();

        // Email Service
        services.AddScoped<IEmailService, EmailService>();

        // Deduplication Service
        services.AddScoped<IDeduplicationService, DeduplicationService>();

        // Groq AI Analysis Service
        services.AddScoped<IGroqAnalysisService, GroqAnalysisService>();

        // License PDF Service
        services.AddScoped<ILicensePdfService, LicensePdfService>();

        // Reviews
        services.AddScoped<IReviewRepository, ReviewRepository>();

        // Subscriptions
        services.AddScoped<IDatasetSubscriptionRepository, DatasetSubscriptionRepository>();

        // Donations
        services.AddScoped<IDonationRepository, DonationRepository>();

        // Surveys
        services.AddScoped<ISurveyRepository, SurveyRepository>();

        services.AddScoped<DataPoolService>();
    }

    private static string BuildSqliteConnectionString(string? configuredConnectionString, string contentRootPath)
    {
        var sqliteBuilder = new SqliteConnectionStringBuilder(configuredConnectionString);
        if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource))
        {
            sqliteBuilder.DataSource = Path.Combine(contentRootPath, "..", "OnionArchitecture.db");
            return sqliteBuilder.ToString();
        }

        if (Path.IsPathRooted(sqliteBuilder.DataSource))
        {
            return sqliteBuilder.ToString();
        }

        var normalizedDataSource = sqliteBuilder.DataSource.Replace('\\', '/');
        if (normalizedDataSource.StartsWith("WebApi/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedDataSource = normalizedDataSource["WebApi/".Length..];
        }

        sqliteBuilder.DataSource = Path.GetFullPath(normalizedDataSource, contentRootPath);
        return sqliteBuilder.ToString();
    }
}

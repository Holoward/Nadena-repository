using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Persistence.Models;

namespace Persistence.Context;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService) : base(options)
    {
        // Use default tracking behavior to allow SaveChanges for auditing
        // QueryTrackingBehavior can be set at query level when needed
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }

    public DbSet<Client> Clientes { get; set; }
    public DbSet<Volunteer> Volunteers { get; set; }
    public DbSet<Buyer> Buyers { get; set; }
    public DbSet<Dataset> Datasets { get; set; }
    public DbSet<ConsentRecord> ConsentRecords { get; set; }
    public DbSet<YoutubeComment> YoutubeComments { get; set; }
    public DbSet<DatasetPurchase> DatasetPurchases { get; set; }
    public DbSet<VolunteerPayment> VolunteerPayments { get; set; }
    public DbSet<SpotifyListeningRecord> SpotifyListeningRecords { get; set; }
    public DbSet<NetflixViewingRecord> NetflixViewingRecords { get; set; }

    // B2B Licensing
    public DbSet<DataPool> DataPools { get; set; }
    public DbSet<DataLicense> DataLicenses { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    // Audit Logging
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<LedgerTransaction> Transactions { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<DeletionRequest> DeletionRequests { get; set; }
    public DbSet<DatasetAccessGrant> DatasetAccessGrants { get; set; }
    public DbSet<ContributorDisbursement> ContributorDisbursements { get; set; }

    // Reviews
    public DbSet<Review> Reviews { get; set; }

    // Subscriptions
    public DbSet<DatasetSubscription> DatasetSubscriptions { get; set; }

    // Google Takeout — YouTube Watch History
    public DbSet<WatchEvent> WatchEvents { get; set; }

    // Donations
    public DbSet<Donation> Donations { get; set; }

    // Contributor Emails (separate from donations - NEVER exported)
    public DbSet<ContributorEmail> ContributorEmails { get; set; }

    // Surveys
    public DbSet<SurveyTemplate> SurveyTemplates { get; set; }
    public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
    public DbSet<SurveyResponse> SurveyResponses { get; set; }

    private string GetCurrentUser()
    {
        return _currentUserService.GetCurrentUserName() ?? "System";
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var currentUser = GetCurrentUser();

        // this foreach is for update the Created and lastModified props in all the entities that inherit from AuditableBaseEntity when they save the changes
        foreach (var entry in ChangeTracker.Entries<AuditableBaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Created = _dateTimeService.NowUtc;
                    entry.Entity.CreatedBy = currentUser;
                    entry.Entity.LastModifiedBY = currentUser;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModified = _dateTimeService.NowUtc;
                    entry.Entity.LastModifiedBY = currentUser;
                    break;
            }
        }

        // Handle AuditableBaseEntityGuid entities
        foreach (var entry in ChangeTracker.Entries<AuditableBaseEntityGuid>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Created = _dateTimeService.NowUtc;
                    entry.Entity.CreatedBy = currentUser;
                    entry.Entity.LastModifiedBY = currentUser;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModified = _dateTimeService.NowUtc;
                    entry.Entity.LastModifiedBY = currentUser;
                    break;
            }
        }

        // Append-only enforcement for transactions
        var mutatedTransactions = ChangeTracker.Entries<LedgerTransaction>()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();
        if (mutatedTransactions.Any())
        {
            throw new InvalidOperationException("Ledger transactions are append-only.");
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

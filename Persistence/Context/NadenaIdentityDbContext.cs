using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Persistence.Models;

namespace Persistence.Context;

public sealed class NadenaIdentityDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;

    public NadenaIdentityDbContext(
        DbContextOptions<NadenaIdentityDbContext> options,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService) : base(options)
    {
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
    }

    public DbSet<Volunteer> Volunteers { get; set; }
    public DbSet<Buyer> Buyers { get; set; }
    public DbSet<ConsentRecord> ConsentRecords { get; set; }
    public DbSet<VolunteerPayment> VolunteerPayments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<LedgerTransaction> Transactions { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }
    public DbSet<DeletionRequest> DeletionRequests { get; set; }
    public DbSet<ContributorDisbursement> ContributorDisbursements { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<ContributorEmail> ContributorEmails { get; set; }
    public DbSet<ContributorOAuthToken> ContributorOAuthTokens { get; set; }
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

        // Audit for int-based entities
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

        // Audit for Guid-based entities
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


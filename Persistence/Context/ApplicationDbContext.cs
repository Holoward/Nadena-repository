using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public sealed class ApplicationDbContext : DbContext
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
        builder.Entity<Wallet>()
            .HasIndex(w => w.OwnerId)
            .IsUnique()
            .HasDatabaseName("IX_Wallets_OwnerId_Unique");
    }

    public DbSet<Dataset> Datasets { get; set; }
    public DbSet<DatasetPurchase> DatasetPurchases { get; set; }
    public DbSet<SpotifyListeningRecord> SpotifyListeningRecords { get; set; }
    public DbSet<NetflixViewingRecord> NetflixViewingRecords { get; set; }

    // B2B Licensing
    public DbSet<DataPool> DataPools { get; set; }
    public DbSet<DataLicense> DataLicenses { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    public DbSet<DatasetAccessGrant> DatasetAccessGrants { get; set; }

    // Subscriptions
    public DbSet<DatasetSubscription> DatasetSubscriptions { get; set; }

    // Google Takeout — YouTube Watch History
    public DbSet<WatchEvent> WatchEvents { get; set; }

    // Donations
    public DbSet<Donation> Donations { get; set; }


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


        return base.SaveChangesAsync(cancellationToken);
    }
}


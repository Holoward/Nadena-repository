using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Seeders;

/// <summary>
/// Seeds / upserts the canonical DataPool entries for the B2B marketplace.
/// Runs on every startup so that name and description changes propagate to
/// already-initialised databases — not just fresh ones.
/// </summary>
public static class DataPoolSeeder
{
    private record PoolDefinition(
        string Name,
        string Description,
        string Category,
        decimal PricePerMonth,
        string SourceTable);

    private static readonly PoolDefinition[] Definitions =
    [
        new(
            Name: "YouTube Behavioral Dataset",
            Description: "Anonymized YouTube viewing behavior from consented contributors. " +
                         "Includes session structure, category distributions, hour-of-day patterns, " +
                         "and day-of-week patterns. No titles, channel names, or URLs stored. " +
                         "Suitable for recommendation-system training, media-consumption research, " +
                         "and behavioral AI tasks.",
            Category: "Social Media",
            PricePerMonth: 99.00m,
            SourceTable: "WatchEvents"),

        new(
            Name: "Spotify Listening Behavioral Dataset",
            Description: "Anonymized Spotify listening behavior from consented contributors. " +
                         "Includes listening-session duration, skip patterns, and genre distributions. " +
                         "No track names or artist names stored. " +
                         "Suitable for music-consumption research and audio-preference modeling.",
            Category: "Music",
            PricePerMonth: 79.00m,
            SourceTable: "SpotifyListeningRecords"),

        new(
            Name: "Netflix Viewing Behavioral Dataset",
            Description: "Anonymized Netflix viewing behavior from consented contributors. " +
                         "Includes viewing-session duration, category distributions, device-type patterns, " +
                         "and completion rates. No titles or show names stored. " +
                         "Suitable for streaming-consumption research.",
            Category: "Streaming",
            PricePerMonth: 89.00m,
            SourceTable: "NetflixViewingRecords"),
    ];

    public static async Task SeedDataPoolsAsync(ApplicationDbContext context)
    {
        foreach (var def in Definitions)
        {
            var existing = await context.DataPools
                .FirstOrDefaultAsync(p => p.Name == def.Name);

            if (existing is null)
            {
                await context.DataPools.AddAsync(new Domain.Entities.DataPool
                {
                    Name = def.Name,
                    Description = def.Description,
                    Category = def.Category,
                    PricePerMonth = def.PricePerMonth,
                    RevenueSharePercent = 40m,
                    IsActive = true,
                    SourceTable = def.SourceTable,
                    Created = DateTime.UtcNow,
                    CreatedBy = "System",
                    LastModified = DateTime.UtcNow,
                    LastModifiedBY = "System"
                });
            }
            else
            {
                // Keep user-overridable fields (PricePerMonth, RevenueSharePercent, IsActive)
                // but always sync canonical metadata.
                existing.Description = def.Description;
                existing.Category = def.Category;
                existing.SourceTable = def.SourceTable;
                existing.LastModified = DateTime.UtcNow;
                existing.LastModifiedBY = "System";
                context.DataPools.Update(existing);
            }
        }

        await context.SaveChangesAsync();
    }
}

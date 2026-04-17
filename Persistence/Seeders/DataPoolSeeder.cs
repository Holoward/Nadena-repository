using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Seeders;

/// <summary>
/// Seeds initial DataPool entries for the B2B marketplace
/// </summary>
public static class DataPoolSeeder
{
    public static async Task SeedDataPoolsAsync(ApplicationDbContext context)
    {
        // Check if DataPools already exist
        if (await context.DataPools.AnyAsync())
            return;

        var dataPools = new List<Domain.Entities.DataPool>
        {
            new Domain.Entities.DataPool
            {
                Name = "YouTube Comment Behavior DAO",
                Description = "Aggregated YouTube comment behavior data including engagement patterns, sentiment trends, and viewer interaction metrics. Perfect for training AI models on social media consumer behavior.",
                Category = "Social Media",
                PricePerMonth = 99.00m,
                RevenueSharePercent = 75.00m,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = "System",
                LastModified = DateTime.UtcNow,
                LastModifiedBY = "System"
            },
            new Domain.Entities.DataPool
            {
                Name = "Consumer Preferences DAO",
                Description = "E-commerce consumer preference data including purchase patterns, product browsing behavior, and price sensitivity metrics. Ideal for recommendation system training and market analysis.",
                Category = "Commerce",
                PricePerMonth = 149.00m,
                RevenueSharePercent = 70.00m,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = "System",
                LastModified = DateTime.UtcNow,
                LastModifiedBY = "System"
            },
            new Domain.Entities.DataPool
            {
                Name = "Health & Wellness Trends DAO",
                Description = "Health and wellness data including fitness tracking, dietary preferences, and wellness product trends. Valuable for health AI applications and market research.",
                Category = "Health",
                PricePerMonth = 199.00m,
                RevenueSharePercent = 80.00m,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = "System",
                LastModified = DateTime.UtcNow,
                LastModifiedBY = "System"
            }
        };

        await context.DataPools.AddRangeAsync(dataPools);
        await context.SaveChangesAsync();
    }
}

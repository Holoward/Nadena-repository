using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Dataset : AuditableBaseEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int VolunteerCount { get; set; }
    public int CommentCount { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; }
    public string BuyerReference { get; set; }
    public string? DataIntegrityHash { get; set; }
    public IntegrityStatus IntegrityStatus { get; set; } = IntegrityStatus.Pending;
    public string? IntegrityReason { get; set; }

    /// <summary>Optional link to a DataPool for B2B licensing. Null = standalone dataset.</summary>
    public int? DataPoolId { get; set; }

    /// <summary>JSON-serialized analysis result from Groq AI. Null = not analyzed.</summary>
    public string? AnalysisResult { get; set; }

    // Enhanced metadata fields
    public string Language { get; set; } = "English";
    public string GeographicCoverage { get; set; } = "Global";
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public string UpdateFrequency { get; set; } = "OneTime";
    public string? IntendedUseCases { get; set; }
    public string DataFormat { get; set; } = "CSV";
    public string? SchemaDescription { get; set; }

    // Subscription pricing
    public string PricingModel { get; set; } = "OneTime";
    public decimal? MonthlyPrice { get; set; }
    public decimal? QuarterlyPrice { get; set; }
}

namespace Application.DTOs;

public class DatasetDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int VolunteerCount { get; set; }
    public int CommentCount { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; }
    public string BuyerReference { get; set; }
    public DateTime CreatedAt { get; set; }

    // Enhanced metadata fields
    public string Language { get; set; }
    public string GeographicCoverage { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public string UpdateFrequency { get; set; }
    public string? IntendedUseCases { get; set; }
    public string DataFormat { get; set; }
    public string? SchemaDescription { get; set; }

    // Review aggregates
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    // Provenance
    public string? ProvenanceDownloadUrl { get; set; }
}

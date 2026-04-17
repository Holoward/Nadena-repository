namespace Application.Features.Datasets.Commands.BuildDataset;

/// <summary>
/// Plain POCO representing the provenance document for a dataset
/// </summary>
public class ProvenanceDocument
{
    public int DatasetId { get; set; }
    public DateTime BuiltAt { get; set; }
    public int VolunteerCount { get; set; }
    public int CommentCount { get; set; }
    public bool ConsentRecordsAvailable { get; set; }
    public string AnonymizationMethod { get; set; } = "K-anonymity with field suppression";
    public string PipelineVersion { get; set; } = "1.0.0";
    public string DataCollectionMethod { get; set; } = "Voluntary Google Takeout export";
}

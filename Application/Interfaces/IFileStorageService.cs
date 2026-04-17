namespace Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveDatasetCsv(string datasetId, string csvContent);

    /// <summary>
    /// Saves a provenance JSON document alongside the dataset CSV
    /// </summary>
    Task<string> SaveProvenanceJson(string datasetId, string jsonContent);
}

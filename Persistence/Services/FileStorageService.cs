using Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;

namespace Persistence.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveDatasetCsv(string datasetId, string csvContent)
    {
        var datasetsPath = Path.Combine(_env.WebRootPath, "datasets");
        if (!Directory.Exists(datasetsPath))
        {
            Directory.CreateDirectory(datasetsPath);
        }

        var csvFileName = $"{datasetId}.csv";
        var csvFilePath = Path.Combine(datasetsPath, csvFileName);
        
        await File.WriteAllTextAsync(csvFilePath, csvContent);
        
        return $"/datasets/{csvFileName}";
    }

    public async Task<string> SaveProvenanceJson(string datasetId, string jsonContent)
    {
        var datasetsPath = Path.Combine(_env.WebRootPath, "datasets");
        if (!Directory.Exists(datasetsPath))
        {
            Directory.CreateDirectory(datasetsPath);
        }

        var provenanceFileName = $"provenance_{datasetId}.json";
        var provenanceFilePath = Path.Combine(datasetsPath, provenanceFileName);

        await File.WriteAllTextAsync(provenanceFilePath, jsonContent);

        return $"/datasets/{provenanceFileName}";
    }
}

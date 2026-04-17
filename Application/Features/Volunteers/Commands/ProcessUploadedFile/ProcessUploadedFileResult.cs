namespace Application.Features.Volunteers.Commands.ProcessUploadedFile;

public class ProcessUploadedFileResult
{
    public int TotalCommentsProcessed { get; set; }
    public string? WarningMessage { get; set; }
    public string? IntegrityHash { get; set; }
    public string? IntegrityStatus { get; set; }
    public string? IntegrityReason { get; set; }
}

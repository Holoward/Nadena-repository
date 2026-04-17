namespace Application.Features.Volunteers.DTOs;

public class DeduplicationResultDto
{
    public bool IsAccepted { get; set; }
    public decimal NewContentPercentage { get; set; }
    public int NewCommentCount { get; set; }
    public int DuplicateCommentCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal RequiredNewContentPercent { get; set; } = 60.0m;
}

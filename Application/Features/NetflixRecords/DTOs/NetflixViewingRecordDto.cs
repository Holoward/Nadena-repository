namespace Application.Features.NetflixRecords.DTOs;

public class NetflixViewingRecordDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShowTitle { get; set; } = string.Empty;
    public DateTime WatchedDate { get; set; }
    public int DurationMinutes { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsAnonymized { get; set; }
}

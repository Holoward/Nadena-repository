namespace Application.Features.SpotifyRecords.DTOs;

public class SpotifyListeningRecordDto
{
    public int Id { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public DateTime PlayedAt { get; set; }
    public int MsPlayed { get; set; }
    public bool IsAnonymized { get; set; }
    public string Platform { get; set; } = string.Empty;
}

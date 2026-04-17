using Domain.Common;

namespace Domain.Entities;

public class SpotifyListeningRecord : AuditableBaseEntity
{
    public int VolunteerId { get; set; }
    public string TrackName { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public DateTime PlayedAt { get; set; }
    public int MsPlayed { get; set; }
    public bool IsAnonymized { get; set; }
    public string Platform { get; set; } = string.Empty;
}

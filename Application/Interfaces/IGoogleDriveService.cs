namespace Application.Interfaces;

public class DriveFile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
    public long Size { get; set; }
}

public interface IGoogleDriveService
{
    Task<string> RefreshAccessTokenAsync(string encryptedRefreshToken);
    Task<List<DriveFile>> FindTakeoutFilesAsync(string accessToken, DateTime newerThan);
    Task<Stream> DownloadFileAsync(string accessToken, string fileId);
}

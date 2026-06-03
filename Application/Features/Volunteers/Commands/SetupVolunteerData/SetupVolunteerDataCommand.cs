using Application.Exceptions;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;
using System.Text.Json;

namespace Application.Features.Volunteers.Commands.SetupVolunteerData;

public class SetupVolunteerDataCommand : IRequest<ServiceResponse<bool>>
{
    public string UserId { get; set; } = string.Empty;
    public List<string> DataSources { get; set; } = new();
    public string? YouTubeDetails { get; set; }
    public string? SpotifyDetails { get; set; }
    public string? NetflixDetails { get; set; }
}

public class SetupVolunteerDataCommandHandler : IRequestHandler<SetupVolunteerDataCommand, ServiceResponse<bool>>
{
    private readonly IVolunteerRepository _volunteerRepository;

    public SetupVolunteerDataCommandHandler(IVolunteerRepository volunteerRepository)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<ServiceResponse<bool>> Handle(SetupVolunteerDataCommand request, CancellationToken cancellationToken)
    {
        var volunteer = await _volunteerRepository.GetByUserIdAsync(request.UserId);
        if (volunteer == null) throw new ApiException($"Data Contributor profile not found with UserId {request.UserId}");

        volunteer.ContentTypes = string.Join(", ", request.DataSources);
        
        var notesObj = new {
            YouTube = request.YouTubeDetails,
            Spotify = request.SpotifyDetails,
            Netflix = request.NetflixDetails
        };
        
        volunteer.Notes = JsonSerializer.Serialize(notesObj);
        
        if (request.DataSources.Contains("YouTube", StringComparer.OrdinalIgnoreCase) && !string.IsNullOrEmpty(request.YouTubeDetails))
        {
            try 
            {
                var yt = JsonSerializer.Deserialize<Dictionary<string, string>>(request.YouTubeDetails);
                if (yt != null)
                {
                    if (yt.TryGetValue("accountAge", out var age)) volunteer.YouTubeAccountAge = age;
                    if (yt.TryGetValue("commentCount", out var count)) volunteer.CommentCountEstimate = count;
                }
            } 
            catch { }
        }

        await _volunteerRepository.UpdateAsync(volunteer);

        return new ServiceResponse<bool>(true, "Data contributor setup completed successfully.");
    }
}

using Application.Wrappers;
using MediatR;

namespace Application.Features.Volunteers.Commands.ProcessUploadedFile;

public class ProcessUploadedFileCommand : IRequest<ServiceResponse<ProcessUploadedFileResult>>
{
    public int VolunteerId { get; set; }
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

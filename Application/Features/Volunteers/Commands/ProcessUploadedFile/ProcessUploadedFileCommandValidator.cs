using FluentValidation;

namespace Application.Features.Volunteers.Commands.ProcessUploadedFile;

public class ProcessUploadedFileCommandValidator : AbstractValidator<ProcessUploadedFileCommand>
{
    public ProcessUploadedFileCommandValidator()
    {
        RuleFor(x => x.VolunteerId)
            .GreaterThan(0)
            .WithMessage("Valid volunteer ID is required");

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("File stream is required");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .Must(BeAZipOrJsonFile)
            .WithMessage("File must be a ZIP archive (.zip) or a JSON file (.json)");
    }

    private bool BeAZipOrJsonFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var ext = Path.GetExtension(fileName);
        return ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".json", StringComparison.OrdinalIgnoreCase);
    }
}

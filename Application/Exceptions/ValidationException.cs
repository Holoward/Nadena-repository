using FluentValidation.Results;

namespace Application.Exceptions;

public class ValidationException : Exception
{
    public List<string> Errors { get; }
    
    public ValidationException() : base("Something went wrong, there are more than one validation problem")
    {
        Errors = new List<string>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        foreach (var failure in failures)
        {
            Errors.Add(failure.ErrorMessage);
        }
    }

}
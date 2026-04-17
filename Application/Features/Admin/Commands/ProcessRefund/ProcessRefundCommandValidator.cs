using FluentValidation;

namespace Application.Features.Admin.Commands.ProcessRefund;

public class ProcessRefundCommandValidator : AbstractValidator<ProcessRefundCommand>
{
    public ProcessRefundCommandValidator()
    {
        RuleFor(x => x.PurchaseId)
            .NotEmpty().WithMessage("Purchase ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason for refund is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}

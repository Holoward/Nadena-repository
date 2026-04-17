using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Commands.ActivateSurvey;

public class ActivateSurveyCommand : IRequest<ServiceResponse<bool>>
{
    public int SurveyTemplateId { get; set; }
}
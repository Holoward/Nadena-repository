using Application.Features.Survey.DTOs;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Commands.SubmitSurveyResponse;

public class SubmitSurveyResponseCommand : IRequest<ServiceResponse<bool>>
{
    public string ContributorId { get; set; } = string.Empty;
    public int SurveyTemplateId { get; set; }
    public List<SurveyResponseDto> Responses { get; set; } = new();
}
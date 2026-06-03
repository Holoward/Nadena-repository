using Application.Features.Survey.DTOs;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Commands.CreateSurvey;

public class CreateSurveyCommand : IRequest<ServiceResponse<SurveyTemplateDto>>
{
    public string ResearcherId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public List<CreateSurveyQuestionDto> Questions { get; set; } = new();
}
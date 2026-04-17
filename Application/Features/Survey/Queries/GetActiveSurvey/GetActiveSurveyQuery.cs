using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Queries.GetActiveSurvey;

public class GetActiveSurveyQuery : IRequest<ServiceResponse<Application.Features.Survey.DTOs.SurveyTemplateDto?>>
{
}
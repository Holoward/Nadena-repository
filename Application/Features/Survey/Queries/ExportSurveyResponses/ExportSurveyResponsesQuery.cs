using Application.Wrappers;
using MediatR;

namespace Application.Features.Survey.Queries.ExportSurveyResponses;

public class ExportSurveyResponsesQuery : IRequest<ServiceResponse<List<Application.Features.Survey.DTOs.SurveyExportRow>>>
{
    public int SurveyTemplateId { get; set; }
}
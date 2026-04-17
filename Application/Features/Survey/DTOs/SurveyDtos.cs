using Domain.Enums;

namespace Application.Features.Survey.DTOs;

public class SurveyTemplateDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<SurveyQuestionDto> Questions { get; set; } = new();
}

public class SurveyQuestionDto
{
    public int Id { get; set; }
    public int OrderIndex { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public SurveyQuestionType QuestionType { get; set; }
    public string? Options { get; set; }
    public int? ScaleMin { get; set; }
    public int? ScaleMax { get; set; }
    public string? ScaleMinLabel { get; set; }
    public string? ScaleMaxLabel { get; set; }
    public bool IsRequired { get; set; }
}

public class CreateSurveyQuestionDto
{
    public int OrderIndex { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public SurveyQuestionType QuestionType { get; set; }
    public string? Options { get; set; }
    public int? ScaleMin { get; set; }
    public int? ScaleMax { get; set; }
    public string? ScaleMinLabel { get; set; }
    public string? ScaleMaxLabel { get; set; }
    public bool IsRequired { get; set; }
}

public class SubmitSurveyRequest
{
    public int SurveyTemplateId { get; set; }
    public List<SurveyResponseDto> Responses { get; set; } = new();
}

public class SurveyResponseDto
{
    public int QuestionId { get; set; }
    public string ResponseValue { get; set; } = string.Empty;
}

public class SurveyExportRow
{
    public string ContributorId { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string ResponseValue { get; set; } = string.Empty;
    public DateTime RespondedAt { get; set; }
}
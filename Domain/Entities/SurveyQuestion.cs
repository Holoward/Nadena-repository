using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class SurveyQuestion : AuditableBaseEntity
{
    public int SurveyTemplateId { get; set; }
    public int OrderIndex { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public SurveyQuestionType QuestionType { get; set; }
    public string? Options { get; set; }
    public int? ScaleMin { get; set; }
    public int? ScaleMax { get; set; }
    public string? ScaleMinLabel { get; set; }
    public string? ScaleMaxLabel { get; set; }
    public bool IsRequired { get; set; }

    public SurveyTemplate SurveyTemplate { get; set; } = null!;
}
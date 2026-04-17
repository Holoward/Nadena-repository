using Domain.Common;

namespace Domain.Entities;

public class SurveyResponse : AuditableBaseEntity
{
    public string ContributorId { get; set; } = string.Empty;
    public int SurveyTemplateId { get; set; }
    public int QuestionId { get; set; }
    public string ResponseValue { get; set; } = string.Empty;
    public DateTime RespondedAt { get; set; }

    public SurveyTemplate SurveyTemplate { get; set; } = null!;
    public SurveyQuestion Question { get; set; } = null!;
}
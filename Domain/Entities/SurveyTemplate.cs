using Domain.Common;

namespace Domain.Entities;

public class SurveyTemplate : AuditableBaseEntity
{
    public string ResearcherId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Version { get; set; } = "1.0";

    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
}
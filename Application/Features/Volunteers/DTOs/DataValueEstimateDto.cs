namespace Application.Features.Volunteers.DTOs;

public class DataValueEstimateDto
{
    public decimal EstimatedValue { get; set; }
    public Dictionary<string, decimal> ValueBreakdown { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
}

using Application.Features.Volunteers.DTOs;
using Application.Interfaces;
using Application.Wrappers;
using MediatR;

namespace Application.Features.Volunteers.Commands.EstimateDataValue;

public class EstimateDataValueCommandHandler : IRequestHandler<EstimateDataValueCommand, ServiceResponse<DataValueEstimateDto>>
{
    private readonly IVolunteerRepository _volunteerRepository;

    public EstimateDataValueCommandHandler(IVolunteerRepository volunteerRepository)
    {
        _volunteerRepository = volunteerRepository;
    }

    public async Task<ServiceResponse<DataValueEstimateDto>> Handle(EstimateDataValueCommand request, CancellationToken cancellationToken)
    {
        var volunteer = await _volunteerRepository.GetByIdAsync(request.VolunteerId);
        if (volunteer == null)
        {
            return new ServiceResponse<DataValueEstimateDto>("Data Contributor not found");
        }

        var breakdown = new Dictionary<string, decimal>();
        decimal baseValue = 0;
        decimal accountAgeBonus = 0;
        decimal contentTypeBonus = 0;
        decimal qualityMultiplier = 1.0m;

        // Calculate base value: $1.00 per 100 comments (capped at $5.00 base)
        baseValue = Math.Min(request.CommentCountEstimate / 100m, 5.0m);
        breakdown["Base Value"] = baseValue;

        // Calculate account age bonus
        if (!string.IsNullOrEmpty(request.YouTubeAccountAge))
        {
            if (int.TryParse(request.YouTubeAccountAge, out int accountAgeYears))
            {
                if (accountAgeYears >= 5)
                {
                    accountAgeBonus = 1.50m;
                }
                else if (accountAgeYears >= 3)
                {
                    accountAgeBonus = 1.00m;
                }
                else if (accountAgeYears >= 1)
                {
                    accountAgeBonus = 0.50m;
                }
            }
        }
        breakdown["Account Age Bonus"] = accountAgeBonus;

        // Calculate content type bonus
        if (!string.IsNullOrEmpty(request.ContentTypes))
        {
            var contentTypes = request.ContentTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ct => ct.Trim().ToLowerInvariant())
                .ToList();

            foreach (var contentType in contentTypes)
            {
                if (contentType == "technology" || contentType == "science" || contentType == "education")
                {
                    contentTypeBonus += 0.75m;
                }
                else if (contentType == "gaming" || contentType == "music" || contentType == "sports")
                {
                    contentTypeBonus += 0.50m;
                }
                else if (contentType == "comedy" || contentType == "entertainment")
                {
                    contentTypeBonus += 0.25m;
                }
            }
        }
        breakdown["Content Type Bonus"] = contentTypeBonus;

        // Calculate quality multiplier
        if (request.CommentCountEstimate < 50)
        {
            qualityMultiplier = 0.5m;
        }
        else if (request.CommentCountEstimate <= 200)
        {
            qualityMultiplier = 1.0m;
        }
        else if (request.CommentCountEstimate <= 500)
        {
            qualityMultiplier = 1.2m;
        }
        else
        {
            qualityMultiplier = 1.5m;
        }
        breakdown["Quality Multiplier"] = qualityMultiplier;

        // Calculate total before platform fee
        decimal totalBeforeFee = (baseValue + accountAgeBonus + contentTypeBonus) * qualityMultiplier;
        breakdown["Total Before Fee"] = totalBeforeFee;

        // Apply platform fee deduction: subtract 25%
        decimal platformFee = totalBeforeFee * 0.25m;
        decimal estimatedValue = totalBeforeFee - platformFee;
        breakdown["Platform Fee (25%)"] = platformFee;

        // Round to 2 decimal places
        estimatedValue = Math.Round(estimatedValue, 2);

        // Calculate min and max values
        decimal minValue = Math.Round(estimatedValue * 0.8m, 2);
        decimal maxValue = Math.Round(estimatedValue * 1.3m, 2);

        // Store calculated EstimatedValue in Volunteer
        volunteer.DataEstimatedValue = estimatedValue;
        await _volunteerRepository.UpdateAsync(volunteer);

        // Build explanation
        var explanation = $"Estimated value calculated based on {request.CommentCountEstimate} comments, " +
                         $"account age of {request.YouTubeAccountAge ?? "unknown"} years, " +
                         $"and content types: {request.ContentTypes ?? "none"}. " +
                         $"Quality multiplier of {qualityMultiplier}x applied. " +
                         $"Platform fee of 25% deducted.";

        var result = new DataValueEstimateDto
        {
            EstimatedValue = estimatedValue,
            ValueBreakdown = breakdown,
            Explanation = explanation,
            MinValue = minValue,
            MaxValue = maxValue
        };

        return new ServiceResponse<DataValueEstimateDto>(result);
    }
}

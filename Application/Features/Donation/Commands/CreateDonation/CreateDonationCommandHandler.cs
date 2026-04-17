using Application.Exceptions;
using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;
using System.Text.Json;

namespace Application.Features.Donation.Commands.CreateDonation;

public class CreateDonationCommandHandler : IRequestHandler<CreateDonationCommand, ServiceResponse<Guid>>
{
    private readonly IWatchEventRepository _watchEventRepository;
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly IDonationRepository _donationRepository;

    public CreateDonationCommandHandler(
        IWatchEventRepository watchEventRepository,
        IVolunteerRepository volunteerRepository,
        IDonationRepository donationRepository)
    {
        _watchEventRepository = watchEventRepository;
        _volunteerRepository = volunteerRepository;
        _donationRepository = donationRepository;
    }

    public async Task<ServiceResponse<Guid>> Handle(CreateDonationCommand request, CancellationToken cancellationToken)
    {
        var contributorIdStr = request.ContributorId.ToString();
        var volunteer = await _volunteerRepository.GetByUserIdAsync(contributorIdStr);
        if (volunteer == null)
        {
            throw new ApiException("Contributor not found");
        }

        var existingDonation = await _donationRepository.GetByContributorIdAsync(contributorIdStr, cancellationToken);
        if (existingDonation != null)
        {
            throw new ApiException("Contributor has already donated");
        }

        var watchEvents = await _watchEventRepository.GetByContributorIdAsync(request.ContributorId, cancellationToken);
        if (!watchEvents.Any())
        {
            throw new ApiException("No watch events found for this contributor");
        }

        var consentTimestamp = DateTime.UtcNow;
        var orderedEvents = watchEvents.OrderBy(e => e.WatchedAt).ToList();
        var earliest = orderedEvents.First().WatchedAt;
        var latest = orderedEvents.Last().WatchedAt;
        var totalDaysSpan = (latest - earliest).Days;

        var totalVideos = watchEvents.Count;
        var uniqueVideos = watchEvents.Select(e => e.VideoIdHash).Distinct().Count();
        var uniqueChannels = watchEvents.Select(e => e.ChannelIdHash).Distinct().Count();
        var repeatViewRate = totalVideos > 0 ? (double)(totalVideos - uniqueVideos) / totalVideos : 0;

        var hourDistribution = new int[24];
        var dayDistribution = new int[7];
        var monthlyVolume = new int[12];
        var categoryDistribution = new Dictionary<string, int>();
        var channelWatchCounts = new Dictionary<string, int>();

        foreach (var evt in watchEvents)
        {
            hourDistribution[evt.HourOfDay]++;
            dayDistribution[evt.DayOfWeek]++;
            monthlyVolume[evt.Month - 1]++;

            if (!categoryDistribution.ContainsKey(evt.Category))
            {
                categoryDistribution[evt.Category] = 0;
            }
            categoryDistribution[evt.Category]++;

            if (!channelWatchCounts.ContainsKey(evt.ChannelIdHash))
            {
                channelWatchCounts[evt.ChannelIdHash] = 0;
            }
            channelWatchCounts[evt.ChannelIdHash]++;
        }

        var orderedChannels = channelWatchCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(20)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var goldPayload = new
        {
            contributorId = contributorIdStr,
            consentVersion = request.ConsentVersion,
            consentTimestamp = consentTimestamp,
            dataSource = "takeout",
            collectionPeriod = new
            {
                earliest = earliest,
                latest = latest,
                totalDaysSpan
            },
            volume = new
            {
                totalVideos,
                uniqueVideos,
                uniqueChannels,
                repeatViewRate = Math.Round(repeatViewRate, 4)
            },
            temporal = new
            {
                hourDistribution,
                dayDistribution,
                monthlyVolume
            },
            content = new
            {
                categoryDistribution,
                topChannelWatchCounts = orderedChannels
            },
            submittedAt = consentTimestamp
        };

        var payloadJson = JsonSerializer.Serialize(goldPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var donation = new Domain.Entities.Donation
        {
            ContributorId = contributorIdStr,
            PayloadJson = payloadJson,
            SubmittedAt = consentTimestamp,
            ConsentVersion = request.ConsentVersion
        };

        await _donationRepository.AddAsync(donation, cancellationToken);

        volunteer.HasDonated = true;
        await _volunteerRepository.UpdateAsync(volunteer);

        return new ServiceResponse<Guid>(donation.Id);
    }
}
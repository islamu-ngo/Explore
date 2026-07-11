// ABOUTME: JSON serialization benchmark suite for high-traffic Event DTO payloads.
// ABOUTME: Compares source-generated System.Text.Json metadata against reflection-based serialization.

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Event.Benchmarks.Configuration;
using Explore.Application.DTOs.Event;
using Explore.Application.Serialization;

namespace Event.Benchmarks.Benchmarks;

[Config(typeof(ExploreBenchmarkConfig))]
public class SerializationBenchmarks
{
    private EventListDto _eventDto = null!;
    private string _serializedJson = string.Empty;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _eventDto = new EventListDto
        {
            Id = Guid.NewGuid(),
            Title = "Enterprise Architecture Summit 2026",
            Subtitle = "Scaling clean architecture in distributed systems",
            Description = "A technical summit for architecture, CQRS, and performance engineering.",
            Slug = "enterprise-architecture-summit-2026",
            EventTypeId = 2,
            EventTypeFullName = "Conference",
            AudienceGenderId = 1,
            AudienceGenderFullName = "Mixed",
            AudienceAgeId = 3,
            AudienceAgeFullName = "Adults",
            AudienceAgeMinAge = 18,
            AudienceAgeMaxAge = 70,
            ActorId = Guid.NewGuid(),
            ActorDisplayName = "ISLAMU Engineering",
            ActorTypeId = 2,
            ActorTypeFullName = "Organization",
            ActorProfilePictureId = Guid.NewGuid(),
            ActorProfilePictureUri = "https://cdn.openislamu.org/actors/engineering.png",
            Price = 49.99m,
            CurrencyCode = "EUR",
            FeaturedImageId = Guid.NewGuid(),
            FeaturedImageUri = "https://cdn.openislamu.org/events/summit-2026.jpg",
            IsRegistrationRequired = true,
            ExternalRegistrationUrl = "https://event.openislamu.org/register/summit-2026",
            EventStatusId = 2,
            EventStatusFullName = "Published",
            VisibilityTypeId = 1,
            VisibilityTypeFullName = "Public",
            EventFormatId = 1,
            EventFormatFullName = "Hybrid",
            MadhabId = 1,
            MadhabFullName = "General",
            SessionCount = 12,
            FirstSessionDate = new DateOnly(2026, 10, 10),
            LastSessionDate = new DateOnly(2026, 10, 12),
            Timezone = "Europe/Paris",
            TotalViews = 34891,
            IsUserReported = false,
            EventUrl = "https://event.openislamu.org/events/enterprise-architecture-summit-2026",
            TenantId = Guid.NewGuid()
        };

        _serializedJson = JsonSerializer.Serialize(_eventDto, ExploreJsonContext.Default.EventListDto);
    }

    [Benchmark(Baseline = true)]
    public string Serialize_SourceGenerated()
    {
        return JsonSerializer.Serialize(_eventDto, ExploreJsonContext.Default.EventListDto);
    }

    [Benchmark]
    public string Serialize_Reflection()
    {
        return JsonSerializer.Serialize(_eventDto);
    }

    [Benchmark]
    public EventListDto? Deserialize_SourceGenerated()
    {
        return JsonSerializer.Deserialize(_serializedJson, ExploreJsonContext.Default.EventListDto);
    }

    [Benchmark]
    public EventListDto? Deserialize_Reflection()
    {
        return JsonSerializer.Deserialize<EventListDto>(_serializedJson);
    }
}

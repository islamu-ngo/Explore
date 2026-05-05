// ABOUTME: FluentValidation rules for the canonical CreateEventRequest graph contract.
// ABOUTME: Validates create-page visible fields and temp-key references before transactional persistence.

using System;
using System.Collections.Generic;
using System.Linq;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using FluentValidation;

namespace Explore.Application.DTOs.Event.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator(
        IAudienceAgeRepository audienceAgeRepository,
        IAudienceGenderRepository audienceGenderRepository,
        IEventTypeRepository eventTypeRepository,
        IOrganizationRepository organizationRepository,
        IGroupRepository groupRepository,
        IStorageObjectRepository storageObjectRepository,
        IEventTemplateRepository eventTemplateRepository,
        IEventSeriesRepository eventSeriesRepository,
        IEventRegistrationPolicyRepository eventRegistrationPolicyRepository,
        ILocationRepository locationRepository,
        IRegistrationModeRepository registrationModeRepository,
        ILanguageRepository languageRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IScheduleItemKindRepository scheduleItemKindRepository,
        ILocationRoomRepository locationRoomRepository,
        IEventSessionTemplateRepository eventSessionTemplateRepository)
    {
        RuleFor(p => p.Title)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .MaximumLength(200).WithMessage("{PropertyName} must not exceed 200 characters.");

        RuleFor(p => p.Subtitle).MaximumLength(200);
        RuleFor(p => p.Description).MaximumLength(5000).When(p => !string.IsNullOrWhiteSpace(p.Description));
        RuleFor(p => p.Slug).MaximumLength(500).When(p => !string.IsNullOrWhiteSpace(p.Slug));
        RuleFor(p => p.CurrencyCode).MaximumLength(3).When(p => !string.IsNullOrWhiteSpace(p.CurrencyCode));
        RuleFor(p => p.ExternalRegistrationUrl).MaximumLength(500).When(p => !string.IsNullOrWhiteSpace(p.ExternalRegistrationUrl));
        RuleFor(p => p.Timezone).MaximumLength(100).When(p => !string.IsNullOrWhiteSpace(p.Timezone));
        RuleFor(p => p.EventTimeZoneId).MaximumLength(100).When(p => !string.IsNullOrWhiteSpace(p.EventTimeZoneId));
        RuleFor(p => p.EventUrl).MaximumLength(500).When(p => !string.IsNullOrWhiteSpace(p.EventUrl));
        RuleFor(p => p.BackgroundColor).MaximumLength(32).When(p => !string.IsNullOrWhiteSpace(p.BackgroundColor));
        RuleFor(p => p.BackgroundEffect).MaximumLength(64).When(p => !string.IsNullOrWhiteSpace(p.BackgroundEffect));
        RuleFor(p => p.Price).GreaterThanOrEqualTo(0).When(p => p.Price.HasValue);
        RuleFor(p => p.SeriesOrder).GreaterThanOrEqualTo(0).When(p => p.SeriesOrder.HasValue);

        RuleFor(p => p.EventTypeId)
            .MustAsync(async (id, _) => !id.HasValue || await eventTypeRepository.Exists(id.Value))
            .WithMessage("Event type does not exist.");

        RuleFor(p => p.AudienceGenderId)
            .MustAsync(async (id, _) => !id.HasValue || await audienceGenderRepository.Exists(id.Value))
            .WithMessage("Audience gender does not exist.");

        RuleFor(p => p.AudienceAgeId)
            .MustAsync(async (id, _) => !id.HasValue || await audienceAgeRepository.Exists(id.Value))
            .WithMessage("Audience age does not exist.");

        RuleFor(p => p.OrganizationId)
            .MustAsync(async (id, _) => !id.HasValue || await organizationRepository.Exists(id.Value))
            .WithMessage("Organization does not exist.");

        RuleFor(p => p.GroupId)
            .MustAsync(async (id, _) => !id.HasValue || await groupRepository.Exists(id.Value))
            .WithMessage("Group does not exist.");

        RuleFor(p => p)
            .Must(p => !(p.OrganizationId.HasValue && p.GroupId.HasValue))
            .WithMessage("OrganizationId and GroupId cannot both be provided.");

        RuleFor(p => p.FeaturedImageId)
            .MustAsync(async (id, _) => !id.HasValue || await storageObjectRepository.Exists(id.Value))
            .WithMessage("Featured image does not exist.");

        RuleFor(p => p.BackgroundImageId)
            .MustAsync(async (id, _) => !id.HasValue || await storageObjectRepository.Exists(id.Value))
            .WithMessage("Background image does not exist.");

        RuleFor(p => p.TemplateId)
            .MustAsync(async (id, _) => !id.HasValue || await eventTemplateRepository.Exists(id.Value))
            .WithMessage("Event template does not exist.");

        RuleFor(p => p.EventSeriesId)
            .MustAsync(async (id, _) => !id.HasValue || await eventSeriesRepository.Exists(id.Value))
            .WithMessage("Event series does not exist.");


        RuleFor(p => p.RegistrationPolicyId)
            .MustAsync(async (id, _) => !id.HasValue || await eventRegistrationPolicyRepository.Exists(id.Value))
            .WithMessage("Registration policy does not exist.");

        RuleFor(p => p.Sessions)
            .NotEmpty().WithMessage("At least one session is required.");

        RuleForEach(p => p.Sessions).ChildRules(session =>
        {
            session.RuleFor(s => s.Title).MaximumLength(500).When(s => !string.IsNullOrWhiteSpace(s.Title));
            session.RuleFor(s => s.Description).MaximumLength(5000).When(s => !string.IsNullOrWhiteSpace(s.Description));
            session.RuleFor(s => s.Slug).MaximumLength(500).When(s => !string.IsNullOrWhiteSpace(s.Slug));
            session.RuleFor(s => s.StartTime).NotEmpty().WithMessage("Session start time is required.");
            session.RuleFor(s => s.EndTime).NotEmpty().GreaterThan(s => s.StartTime).WithMessage("Session end time must be after start time.");
            session.RuleFor(s => s.MaxAudienceAttendees).GreaterThan(0).When(s => s.MaxAudienceAttendees.HasValue);
            session.RuleFor(s => s.Price).GreaterThanOrEqualTo(0).When(s => s.Price.HasValue);
            session.RuleFor(s => s.CurrencyCode).MaximumLength(3).When(s => !string.IsNullOrWhiteSpace(s.CurrencyCode));
            session.RuleFor(s => s.FeaturedImageId)
                .MustAsync(async (id, _) => !id.HasValue || await storageObjectRepository.Exists(id.Value))
                .WithMessage("Session featured image does not exist.");
        });

        RuleForEach(p => p.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.Label).MaximumLength(200).When(d => !string.IsNullOrWhiteSpace(d.Label));
            day.RuleFor(d => d.Description).MaximumLength(2000).When(d => !string.IsNullOrWhiteSpace(d.Description));
            day.RuleFor(d => d.BannerText).MaximumLength(500).When(d => !string.IsNullOrWhiteSpace(d.BannerText));
            day.RuleFor(d => d.BannerImageId)
                .MustAsync(async (id, _) => !id.HasValue || await storageObjectRepository.Exists(id.Value))
                .WithMessage("Day banner image does not exist.");
        });

        RuleForEach(p => p.Rooms).ChildRules(room =>
        {
            room.RuleFor(r => r.TempKey).NotEmpty().MaximumLength(80);
            room.RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
            room.RuleFor(r => r.Slug).MaximumLength(500).When(r => !string.IsNullOrWhiteSpace(r.Slug));
            room.RuleFor(r => r.Description).MaximumLength(2000).When(r => !string.IsNullOrWhiteSpace(r.Description));
            room.RuleFor(r => r.Capacity).GreaterThan(0).When(r => r.Capacity.HasValue);
        });

        RuleForEach(p => p.AgendaItems).ChildRules(item =>
        {
            item.RuleFor(i => i.Title).NotEmpty().MaximumLength(300);
            item.RuleFor(i => i.Description).MaximumLength(2000).When(i => !string.IsNullOrWhiteSpace(i.Description));
            item.RuleFor(i => i.StartTime).NotEmpty().WithMessage("Agenda item start time is required.");
            item.RuleFor(i => i.EndTime).NotEmpty().GreaterThan(i => i.StartTime).WithMessage("Agenda item end time must be after start time.");
            item.RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
        });

        RuleFor(p => p).Must(HaveUniqueTempKeys).WithMessage("Temp keys must be unique within days, rooms, and sessions.");
        RuleFor(p => p).Must(HaveValidTempReferences).WithMessage("One or more day or room temp-key references are invalid.");
        RuleFor(p => p).Must(HaveRelativePrayerRequirements).WithMessage("Islamic session scheduling requires LocationId, ReferencePrayer, and OffsetMinutes when StartTimeType is RelativeToPrayer.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllLocationsExistAsync(request, locationRepository))
            .WithMessage("One or more locations do not exist.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllRegistrationModesExistAsync(request, registrationModeRepository))
            .WithMessage("One or more session registration modes do not exist.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllLanguagesExistAsync(request, languageRepository))
            .WithMessage("One or more session languages do not exist.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllCategoriesExistAsync(request, categoryRepository))
            .WithMessage("One or more categories do not exist.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllTagsExistAsync(request, tagRepository))
            .WithMessage("One or more tags do not exist.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllAgendaKindsExistAsync(request, scheduleItemKindRepository))
            .WithMessage("One or more agenda item kinds do not exist.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllExistingRoomsExistAsync(request, locationRoomRepository))
            .WithMessage("One or more rooms do not exist.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllExistingRoomsMatchLocationsAsync(request, locationRoomRepository))
            .WithMessage("One or more rooms do not belong to the submitted location.");

        RuleFor(p => p).MustAsync(async (request, _) => await AllSessionTemplatesExistAsync(request, eventSessionTemplateRepository))
            .WithMessage("One or more session templates do not exist.");
    }

    private static bool HaveUniqueTempKeys(CreateEventRequest request)
    {
        return IsUnique(request.Days.Select(d => d.TempKey))
            && IsUnique(request.Rooms.Select(r => r.TempKey))
            && IsUnique(request.Sessions.Select(s => s.TempKey));
    }

    private static bool IsUnique(IEnumerable<string?> keys)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k!.Trim()))
        {
            if (!seen.Add(key)) return false;
        }

        return true;
    }

    private static bool HaveValidTempReferences(CreateEventRequest request)
    {
        var dayKeys = request.Days.Where(d => !string.IsNullOrWhiteSpace(d.TempKey)).Select(d => d.TempKey!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roomKeys = request.Rooms.Select(r => r.TempKey.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return request.Sessions.All(s => IsBlankOrContained(s.DayTempKey, dayKeys) && IsBlankOrContained(s.RoomTempKey, roomKeys))
            && request.AgendaItems.All(i => IsBlankOrContained(i.DayTempKey, dayKeys) && IsBlankOrContained(i.RoomTempKey, roomKeys));
    }

    private static bool IsBlankOrContained(string? value, HashSet<string> allowed) =>
        string.IsNullOrWhiteSpace(value) || allowed.Contains(value.Trim());

    private static bool HaveRelativePrayerRequirements(CreateEventRequest request)
    {
        return request.Sessions.All(session =>
        {
            if (session.IslamicAspect is null || session.IslamicAspect.StartTimeType == SessionStartTimeType.Fixed)
            {
                return true;
            }

            return session.LocationId.HasValue
                && session.IslamicAspect.ReferencePrayer.HasValue
                && session.IslamicAspect.OffsetMinutes.HasValue;
        });
    }

    private static async Task<bool> AllLocationsExistAsync(CreateEventRequest request, ILocationRepository repository)
    {
        var ids = request.Sessions.Select(s => s.LocationId)
            .Concat(request.Rooms.Select(r => (Guid?)r.LocationId))
            .Concat(request.AgendaItems.Select(i => i.LocationId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();

        foreach (var id in ids)
        {
            if (!await repository.Exists(id)) return false;
        }

        return true;
    }

    private static async Task<bool> AllRegistrationModesExistAsync(CreateEventRequest request, IRegistrationModeRepository repository)
    {
        foreach (var id in request.Sessions.Select(s => s.RegistrationModeId).Where(id => id.HasValue).Select(id => id!.Value).Distinct())
        {
            if (!await repository.Exists(id)) return false;
        }

        return true;
    }

    private static async Task<bool> AllLanguagesExistAsync(CreateEventRequest request, ILanguageRepository repository)
    {
        foreach (var id in request.Sessions.SelectMany(s => s.LanguageIds).Distinct())
        {
            if (!await repository.Exists(id)) return false;
        }

        return true;
    }

    private static async Task<bool> AllCategoriesExistAsync(CreateEventRequest request, ICategoryRepository repository)
    {
        foreach (var id in request.CategoryIds.Distinct())
        {
            if (!await repository.Exists(id)) return false;
        }

        return true;
    }

    private static async Task<bool> AllTagsExistAsync(CreateEventRequest request, ITagRepository repository)
    {
        foreach (var id in request.TagIds.Distinct())
        {
            if (!await repository.Exists(id)) return false;
        }

        return true;
    }

    private static async Task<bool> AllAgendaKindsExistAsync(CreateEventRequest request, IScheduleItemKindRepository repository)
    {
        foreach (var id in request.AgendaItems.Select(i => i.KindId).Where(id => id.HasValue).Select(id => id!.Value).Distinct())
        {
            if (!await repository.Exists(id)) return false;
        }

        return true;
    }

    private static async Task<bool> AllExistingRoomsExistAsync(CreateEventRequest request, ILocationRoomRepository repository)
    {
        foreach (var id in GetExistingRoomIds(request))
        {
            if (!await repository.Exists(id)) return false;
        }

        return true;
    }

    private static async Task<bool> AllExistingRoomsMatchLocationsAsync(CreateEventRequest request, ILocationRoomRepository repository)
    {
        foreach (var (roomId, locationId) in GetExistingRoomLocationPairs(request))
        {
            var room = await repository.GetById(roomId);
            if (room is null || room.LocationId != locationId) return false;
        }

        return true;
    }

    private static async Task<bool> AllSessionTemplatesExistAsync(CreateEventRequest request, IEventSessionTemplateRepository repository)
    {
        foreach (var id in request.Sessions.Select(s => s.SessionTemplateId).Where(id => id.HasValue).Select(id => id!.Value).Distinct())
        {
            if (!await repository.Exists(id)) return false;
        }

        return true;
    }

    private static IEnumerable<Guid> GetExistingRoomIds(CreateEventRequest request) =>
        request.Sessions.Select(s => s.RoomId)
            .Concat(request.AgendaItems.Select(i => i.RoomId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();

    private static IEnumerable<(Guid RoomId, Guid LocationId)> GetExistingRoomLocationPairs(CreateEventRequest request)
    {
        foreach (var session in request.Sessions.Where(s => s.RoomId.HasValue && s.LocationId.HasValue))
        {
            yield return (session.RoomId!.Value, session.LocationId!.Value);
        }

        foreach (var agendaItem in request.AgendaItems.Where(i => i.RoomId.HasValue && i.LocationId.HasValue))
        {
            yield return (agendaItem.RoomId!.Value, agendaItem.LocationId!.Value);
        }
    }

}

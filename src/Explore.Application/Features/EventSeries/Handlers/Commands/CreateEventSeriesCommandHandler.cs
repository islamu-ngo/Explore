// ABOUTME: Handler for creating new event series with validation and tenant context.
// ABOUTME: Validates input, sets tenant, initializes defaults, and generates slug if not provided.

using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSeries.Validators;
using Explore.Application.Features.EventSeries.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSeries.Handlers.Commands;

public class CreateEventSeriesCommandHandler : IRequestHandler<CreateEventSeriesCommand, BaseCommandResponse<Guid>>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IMapper _mapper;

    public CreateEventSeriesCommandHandler(
        IEventSeriesRepository eventSeriesRepository,
        ITenantContext tenantContext,
        IMapper mapper)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _tenantContext = tenantContext;
        _mapper = mapper;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateEventSeriesCommand request, CancellationToken cancellationToken)
    {
        var validator = new CreateEventSeriesDtoValidator();
        var validationResult = await validator.ValidateAsync(request.EventSeriesDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseCommandResponse<Guid>
            {
                Success = false,
                Message = "Event series creation failed due to validation errors.",
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };
        }

        var series = _mapper.Map<Domain.EventSeries>(request.EventSeriesDto);
        series.TenantId = _tenantContext.TenantId;
        series.TotalViews = 0;
        series.VisibilityTypeId = 1; // Default: Public

        if (string.IsNullOrWhiteSpace(series.Slug))
        {
            series.Slug = GenerateSlug(series.Title);
        }

        series = await _eventSeriesRepository.Create(series);

        return new BaseCommandResponse<Guid>
        {
            Id = series.Id,
            Success = true,
            Message = "Event series created successfully."
        };
    }

    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return $"series-{Guid.CreateVersion7().ToString("N")[..8]}";

        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(".", "")
            .Replace(",", "");

        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        if (slug.Length > 50)
            slug = slug[..50];

        return slug;
    }
}

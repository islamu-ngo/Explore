using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventCategories.Validators;

public class CreateEventCategoriesDtoValidator : AbstractValidator<CreateEventCategoriesDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IEventCategoriesRepository _eventCategoriesRepository;

    public CreateEventCategoriesDtoValidator(
        IEventRepository eventRepository,
        ICategoryRepository categoryRepository,
        IEventCategoriesRepository eventCategoriesRepository)
    {
        _eventRepository = eventRepository;
        _categoryRepository = categoryRepository;
        _eventCategoriesRepository = eventCategoriesRepository;

        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MustAsync(EventExists)
            .WithMessage("{PropertyName} not found");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("{PropertyName} is required")
            .MustAsync(CategoryExists)
            .WithMessage("{PropertyName} not found");

        // TenantId is set by the handler from context, not by the client
        // No validation needed here

        RuleFor(x => x)
            .MustAsync(EventCategoryNotExist)
            .WithMessage("This Category is already assigned to this Event");
    }

    private async Task<bool> EventExists(Guid eventId, CancellationToken cancellationToken)
    {
        return await _eventRepository.Exists(eventId);
    }

    private async Task<bool> CategoryExists(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _categoryRepository.Exists(categoryId);
    }

    private async Task<bool> EventCategoryNotExist(CreateEventCategoriesDto dto, CancellationToken cancellationToken)
    {
        return !await _eventCategoriesRepository.Exists(dto.EventId, dto.CategoryId);
    }
}

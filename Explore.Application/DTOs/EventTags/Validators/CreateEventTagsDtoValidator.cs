using System;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using FluentValidation;

namespace Explore.Application.DTOs.EventTags.Validators
{
    public class CreateEventTagsDtoValidator : AbstractValidator<CreateEventTagsDto>
    {
        private readonly IEventRepository _eventRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IEventTagsRepository _eventTagsRepository;

        public CreateEventTagsDtoValidator(
            IEventRepository eventRepository,
            ITagRepository tagRepository,
            IEventTagsRepository eventTagsRepository)
        {
            _eventRepository = eventRepository;
            _tagRepository = tagRepository;
            _eventTagsRepository = eventTagsRepository;

            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(EventExists)
                .WithMessage("{PropertyName} not found");

            RuleFor(x => x.TagId)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .MustAsync(TagExists)
                .WithMessage("{PropertyName} not found");

            // TenantId is set by the handler from context, not by the client
            // No validation needed here

            RuleFor(x => x)
                .MustAsync(EventTagNotExist)
                .WithMessage("This Tag is already assigned to this Event");
        }

        private async Task<bool> EventExists(Guid eventId, CancellationToken cancellationToken)
        {
            return await _eventRepository.Exists(eventId);
        }

        private async Task<bool> TagExists(Guid tagId, CancellationToken cancellationToken)
        {
            return await _tagRepository.Exists(tagId);
        }

        private async Task<bool> EventTagNotExist(CreateEventTagsDto dto, CancellationToken cancellationToken)
        {
            return !await _eventTagsRepository.Exists(dto.EventId, dto.TagId);
        }
    }
}

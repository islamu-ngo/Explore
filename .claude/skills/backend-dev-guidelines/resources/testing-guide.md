# Exemples Complets (.NET 10 / CQRS)

## 1. DTO (Command)
```csharp
// Application/Features/Events/Commands/JoinEvent/JoinEventCommand.cs
public record JoinEventCommand(Guid EventId, Guid UserId) : IRequest<bool>;
2. Validator
// Application/Features/Events/Commands/JoinEvent/JoinEventCommandValidator.cs
public class JoinEventCommandValidator : AbstractValidator<JoinEventCommand>
{
    public JoinEventCommandValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
3. Handler (Business Logic)
// Application/Features/Events/Commands/JoinEvent/JoinEventCommandHandler.cs
public class JoinEventCommandHandler(IEventRepository repository) : IRequestHandler<JoinEventCommand, bool>
{
    public async Task<bool> Handle(JoinEventCommand request, CancellationToken ct)
    {
        var eventEntity = await repository.GetByIdAsync(request.EventId, ct);
        if (eventEntity == null) throw new NotFoundException(nameof(Event), request.EventId);

        if (eventEntity.IsFull) throw new DomainException("Event is full");

        eventEntity.AddParticipant(request.UserId);
        await repository.UpdateAsync(eventEntity, ct);
        
        return true;
    }
}
4. Controller (API)
// Api/Controllers/EventsController.cs
[HttpPost("{id}/join")]
public async Task<IActionResult> Join(Guid id)
{
    var userId = User.GetUserId(); // Extension method pour claims
    var command = new JoinEventCommand(id, userId);
    await _mediator.Send(command);
    return NoContent();
}
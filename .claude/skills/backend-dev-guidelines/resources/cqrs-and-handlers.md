# CQRS & MediatR Handlers

Nous n'utilisons pas de classes "Services" fourre-tout (ex: `EventService`). Nous utilisons des **Handlers** atomiques.

## 📝 Structure d'une Feature
Chaque opération a son propre dossier dans `Application/Features/{Entity}/`.

Exemple : `Application/Features/Events/Commands/CreateEvent/`
*   `CreateEventCommand.cs` (DTO)
*   `CreateEventCommandHandler.cs` (Logique)
*   `CreateEventCommandValidator.cs` (Validation)

## 💻 Exemple de Handler

```csharp
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    
    public CreateEventCommandHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Mapping (Entité du Domain)
        var newEvent = Event.Create(request.Title, request.Description, request.Location);

        // 2. Persistance
        await _eventRepository.AddAsync(newEvent, cancellationToken);

        // 3. Retour
        return newEvent.Id;
    }
}
```
🧠 Query Handlers (Lecture)
Pour la lecture, nous pouvons utiliser AutoMapper pour projeter directement vers des DTOs pour la performance, en utilisant AsNoTracking().

Remplace routing-and-controllers.md.
Fichier : .claude/skills/backend-dev-guidelines/resources/api-and-controllers.md
# API & Controllers Best Practices

Dans notre architecture CQRS, les contrôleurs sont **extrêmement minces**. Ils ne contiennent **aucune logique métier**.

## ❌ Anti-Pattern (À ne pas faire)
```csharp
[HttpPost]
public async Task<IActionResult> CreateEvent(EventDto dto) {
    // ❌ Logique métier dans le contrôleur
    if (dto.Date < DateTime.Now) return BadRequest();
    var entity = new Event { ... };
    _context.Events.Add(entity); // ❌ Accès direct DB
    await _context.SaveChangesAsync();
    return Ok();
}
✅ Le Pattern "Thin Controller" (À faire)
Le contrôleur se contente de dispatcher une commande à MediatR.
[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEventCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
}
📜 Règles
1. Routage : Toujours préfixer /api/v1/.
2. Retour : Utiliser ActionResult<T>.
3. Docs : Ajouter [ProducesResponseType] pour Swagger.
4. Auth : Utiliser [Authorize] si nécessaire.

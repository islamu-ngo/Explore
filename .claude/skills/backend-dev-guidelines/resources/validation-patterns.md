# Validation Patterns - Input Validation (.NET 10)

Guide complet pour la validation type-safe utilisant **FluentValidation** dans une architecture CQRS.

## Basic Patterns

La validation se fait généralement dans la couche **Application**, associée aux `Commands` ou `Queries`.

### Primitive Types

```csharp
public class PrimitiveValidator : AbstractValidator<UserDto>
{
    public PrimitiveValidator()
    {
        // Strings
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(150);

        // Numbers
        RuleFor(x => x.Age)
            .GreaterThan(18)
            .LessThanOrEqualTo(100);

        // Booleans
        RuleFor(x => x.TermsAccepted)
            .Equal(true).WithMessage("Vous devez accepter les conditions.");

        // Dates
        RuleFor(x => x.EventDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("La date doit être dans le futur.");

        // Enums
        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
Objects
// Simple object
RuleFor(x => x.Address).NotNull();

// Nested objects (Utilisation d'un autre validateur)
RuleFor(x => x.Address).SetValidator(new AddressValidator());

// Optional fields (Validation seulement si non null)
RuleFor(x => x.PhoneNumber)
    .Matches(@"^\+[1-9]\d{1,14}$")
    .When(x => x.PhoneNumber != null);

// Nullable fields
RuleFor(x => x.Website)
    .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
    .When(x => !string.IsNullOrEmpty(x.Website));
Arrays
// Array of primitives (ex: List<string>)
RuleForEach(x => x.Tags)
    .NotEmpty()
    .MaximumLength(20);

// Array of objects (ex: List<OrderItem>)
RuleForEach(x => x.Items).SetValidator(new OrderItemValidator());

// Array with constraints
RuleFor(x => x.Items)
    .Must(items => items.Count <= 10).WithMessage("Maximum 10 articles.");
Schema Examples from Codebase
Form Validation Schemas
Dans CQRS, les schémas de validation valident directement les Commandes MediatR.
Fichier : Explore.Application/Features/Forms/Commands/CreateForm/CreateFormCommandValidator.cs
using FluentValidation;
using Explore.Domain.Enums;

namespace Explore.Application.Features.Forms.Commands.CreateForm;

// Question types enum (Défini dans Domain)
// public enum QuestionType { Text, MultipleChoice, FileUpload }

public class CreateFormCommandValidator : AbstractValidator<CreateFormCommand>
{
    public CreateFormCommandValidator()
    {
        // Form section schema
        RuleFor(x => x.Title)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        // Update order schema (via collection)
        RuleForEach(x => x.Sections).SetValidator(new FormSectionValidator());
    }
}

public class FormSectionValidator : AbstractValidator<FormSectionDto>
{
    public FormSectionValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        
        // Question schema
        RuleForEach(x => x.Questions).ChildRules(questions =>
        {
            questions.RuleFor(q => q.Text).NotEmpty();
            questions.RuleFor(q => q.Type).IsInEnum();
            
            // Question option (Conditionnel)
            questions.RuleFor(q => q.Options)
                .NotEmpty()
                .When(q => q.Type == QuestionType.MultipleChoice);
        });
    }
}
Proxy Relationship Schema
Validation des relations entre entités (souvent pour les permissions ou les liens parent/enfant).
// Proxy relationship validation
public class OrganizationRelationshipValidator : AbstractValidator<LinkOrgCommand>
{
    public OrganizationRelationshipValidator()
    {
        RuleFor(x => x.ParentOrgId).NotEmpty();
        RuleFor(x => x.ChildOrgId).NotEmpty();

        // With custom validation (Logique métier simple)
        RuleFor(x => x)
            .Must(x => x.ParentOrgId != x.ChildOrgId)
            .WithMessage("Une organisation ne peut pas être son propre parent.");
    }
}
Workflow Validation
// Workflow start schema
public class StartWorkflowCommandValidator : AbstractValidator<StartWorkflowCommand>
{
    public StartWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId).NotEmpty();
        RuleFor(x => x.InitiatorId).NotEmpty();
    }
}

// Workflow step completion schema
public class CompleteStepCommandValidator : AbstractValidator<CompleteStepCommand>
{
    public CompleteStepCommandValidator()
    {
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.Outcome)
            .Must(o => new[] { "Approved", "Rejected" }.Contains(o))
            .WithMessage("Résultat invalide.");
    }
}
Route-Level Validation
Pattern 1: Inline Validation (Anti-Pattern)
❌ À éviter. Ne pas valider manuellement dans le contrôleur.
// controllers/EventsController.cs
[HttpPost]
public IActionResult Create(EventDto dto)
{
    // A NE PAS FAIRE : Validation manuelle
    if (string.IsNullOrEmpty(dto.Title)) return BadRequest();
    // ...
}
Controller Validation
Pattern 2: Controller Validation (Recommended via MediatR Pipeline)
Dans notre architecture, le contrôleur ne valide pas explicitement. Il envoie la commande, et le Pipeline Behavior lance la validation automatiquement.
Validators/CreateEventCommandValidator.cs
// Définition de la règle
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand> { ... }
Controllers/EventsController.cs
[HttpPost]
public async Task<IActionResult> Create(CreateEventCommand command)
{
    // Le Pipeline MediatR intercepte ici.
    // Si la validation échoue, une ValidationException est levée AVANT d'atteindre ce code.
    var id = await _mediator.Send(command);
    return Ok(id);
}
DTO Pattern
Type Inference from Schemas
Contrairement à TypeScript (Zod), C# est nominal. On définit le type (Record/Class) d'abord, puis le validateur.
// 1. Define schema (Type C#)
public record CreateEventCommand(string Title, DateTime Date, int Capacity);

// 2. Define Validator
public class Validator : AbstractValidator<CreateEventCommand> { ... }

// 3. Use
// L'injection de dépendance scanne l'assembly pour associer IValidator<T> à T.
Input vs Output Types
Ségrégation stricte via CQRS.
// Input schema (what API receives) -> COMMAND
public record RegisterUserCommand(string Email, string Password) : IRequest<Guid>;

// Output schema (what API returns) -> DTO/ViewModel
public record UserResponse(Guid Id, string Email, DateTime CreatedAt);
// Note: Pas de mot de passe dans la réponse !
Error Handling
Error Format
FluentValidation lève une ValidationException. Nous utilisons un middleware global ou un filtre d'exception pour transformer cela en ProblemDetails (RFC 7807).
Custom Error Messages
RuleFor(x => x.Age)
    .GreaterThan(18)
    .WithMessage("Vous devez être majeur pour créer une organisation.");
Formatted Error Response
Exemple de réponse JSON générée automatiquement par l'API en cas d'erreur 400 :
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["L'adresse email est invalide."],
    "Age": ["Vous devez être majeur."]
  }
}
Advanced Patterns
Conditional Validation
// Validate based on other field values
RuleFor(x => x.PassportNumber)
    .NotEmpty()
    .When(x => x.HasPassport == true)
    .WithMessage("Le numéro de passeport est requis si vous avez coché la case.");
Transform Data
En C#, la transformation se fait généralement lors du Binding ou dans le Handler. FluentValidation sert à vérifier, pas à muter. Cependant, on peut normaliser avant :
// Transform strings to numbers : géré automatiquement par le ModelBinding ASP.NET Core
// public int Age { get; set; } // "25" devient 25 automatiquement.
Preprocess Data
// Trim strings before validation
// Astuce : Utiliser un ValueConverter JSON ou le faire dans le constructeur du record
public record CreateTagCommand(string Name)
{
    public string Name { get; init; } = Name?.Trim();
}
Union Types
Simulé en C# via l'héritage ou des champs conditionnels.
// Discriminated unions (Polymorphic Binding)
// ASP.NET Core supporte le polymorphisme dans le body JSON
[JsonDerivedType(typeof(TextQuestion), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ChoiceQuestion), typeDiscriminator: "choice")]
public abstract class QuestionBase { ... }
Recursive Schemas
// For nested structures like trees (Categories)
public class CategoryValidator : AbstractValidator<CategoryDto>
{
    public CategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        // Validation récursive
        RuleForEach(x => x.SubCategories).SetValidator(this);
    }
}
Schema Composition
// Base schemas (Validator commun)
public class IdentityValidator : AbstractValidator<IIdentityInfo>
{
    public IdentityValidator() { RuleFor(x => x.Id).NotEmpty(); }
}

// Extend schemas
public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        // Inclure les règles de base
        Include(new IdentityValidator());
        RuleFor(x => x.Username).NotEmpty();
    }
}
Validation Middleware
C'est le cœur de la validation dans Clean Architecture avec MediatR.
// Create reusable validation pipeline behavior
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }
        return await next();
    }
}

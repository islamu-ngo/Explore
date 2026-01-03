# Middleware & Pipeline Guide (.NET 10)

Ce guide couvre la gestion des préoccupations transversales (Cross-Cutting Concerns) via le **Pipeline HTTP** (ASP.NET Core) et le **Pipeline MediatR** (Clean Architecture).

## 📚 Table des Matières
*   [Le Pipeline HTTP (Program.cs)](#le-pipeline-http-programcs)
*   [Gestion Globale des Erreurs](#gestion-globale-des-erreurs)
*   [Authentification (Keycloak)](#authentification-keycloak)
*   [MediatR Behaviors (Validation & Logging)](#mediatr-behaviors-validation--logging)
*   [Ordre d'Exécution Critique](#ordre-dexécution-critique)

---

## 🚀 Le Pipeline HTTP (Program.cs)

Contrairement à Express.js où tout est middleware, ASP.NET Core utilise un pipeline strict défini dans `Program.cs`.

### Structure Standard
```csharp
var app = builder.Build();

// 1. Gestion des erreurs (Tout en haut)
app.UseExceptionHandler(); 

// 2. Sécurité Transport
app.UseHttpsRedirection();

// 3. Documentation (Swagger)
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. Routage
app.UseRouting();

// 5. CORS
app.UseCors("AllowAll");

// 6. Sécurité Identité (CRITIQUE : AuthN avant AuthZ)
app.UseAuthentication(); // Qui suis-je ? (JWT/Keycloak)
app.UseAuthorization();  // Qu'ai-je le droit de faire ?

// 7. Endpoints
app.MapControllers();

--------------------------------------------------------------------------------
🛡️ Gestion Globale des Erreurs
Ne jamais utiliser de try-catch dans les contrôleurs. Utilisez IExceptionHandler.
Fichier : Explore.Api/Infrastructure/GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Erreur non gérée : {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationEx)
        {
            problemDetails.Title = "Validation Failed";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = "Une ou plusieurs erreurs de validation sont survenues.";
            problemDetails.Extensions["errors"] = validationEx.Errors;
        }
        else if (exception is NotFoundException)
        {
            problemDetails.Title = "Not Found";
            problemDetails.Status = StatusCodes.Status404NotFound;
        }
        else
        {
            problemDetails.Title = "Server Error";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Detail = "Une erreur interne est survenue.";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

--------------------------------------------------------------------------------
🔐 Authentification (Keycloak)
L'authentification est gérée nativement par le package JwtBearer.
Configuration dans Program.cs :
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = "explore-api";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        
        // Validation du Token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = "realm_access" // Mappage spécifique Keycloak
        };
    });

--------------------------------------------------------------------------------
🧠 MediatR Behaviors (Validation & Logging)
Dans la Clean Architecture, la logique métier ne vit pas dans le middleware HTTP, mais dans le pipeline MediatR.
1. Validation Behavior (FluentValidation)
Intercepte chaque commande pour valider les données AVANT d'atteindre le Handler.
Fichier : Explore.Application/Behaviors/ValidationBehavior.cs
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
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
2. Logging Behavior (Audit)
Log automatiquement l'entrée et la sortie de chaque commande avec les IDs de corrélation.
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) 
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("🚀 Début de la commande {RequestName}", requestName);

        var response = await next();

        logger.LogInformation("✅ Fin de la commande {RequestName}", requestName);
        return response;
    }
}
Enregistrement dans DependencyInjection.cs
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    // L'ordre compte aussi ici !
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});

--------------------------------------------------------------------------------
⚠️ Ordre d'Exécution Critique
HTTP Pipeline (Program.cs)
1. Exception Handler (Doit être premier pour tout attraper)
2. Authentication (Doit être avant Authorization)
3. Authorization (Doit être avant les Endpoints)
MediatR Pipeline
1. Logging (Pour voir ce qui entre)
2. Validation (Pour rejeter les données invalides rapidement)
3. Transaction (Pour ouvrir une transaction SQL seulement si valide)
4. Handler (Votre logique métier)

--------------------------------------------------------------------------------
Fichiers Connexes :
• SKILL.md
• validation-patterns.md (Détails sur FluentValidation)
• testing-guide.md (Comment tester les Behaviors)

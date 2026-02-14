# API JWT Validation Patterns

Use this pattern for API projects that accept bearer tokens from an external OIDC provider.

## Registration Pattern

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Auth:Authority"];
        options.Audience = configuration["Auth:Audience"];
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });
```

## Middleware Order

Use the request pipeline in this order:

1. Exception handling
2. Routing and CORS
3. Authentication
4. Authorization
5. Endpoint mapping

Incorrect ordering is a common cause of false `401`/`403` responses.

## Claim Extraction

Normalize user ID extraction with one shared helper/service and fallback order:

1. `sub`
2. `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
3. `sid`

Avoid duplicating claim parsing logic across controllers.

## Logging Guardrails

- Log auth failures with context (issuer, audience mismatch, claim shape).
- Never log raw JWT values.
- Include correlation and trace identifiers for incident triage.

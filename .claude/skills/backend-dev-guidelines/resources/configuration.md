# Configuration & Secrets (.NET)

Nous utilisons le **Options Pattern** et **Infisical** pour la gestion des secrets.

## ❌ Ne faites jamais ça
```csharp
var secret = Environment.GetEnvironmentVariable("Keycloak__ClientSecret"); // NON
✅ Options Pattern
1. Définir une classe :
2. Enregistrer dans Program.cs :
3. Injecter :
🔐 Infisical
En production, les secrets sont injectés via le provider Infisical connecté au Host Builder.

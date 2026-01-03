# Observabilité (OpenTelemetry & Aspire)

ISLAMU Event utilise **.NET Aspire** pour l'orchestration et l'observabilité.

## 📊 Logs (Serilog)
Utilisez le logging structuré injecté :

```csharp
_logger.LogInformation("Création de l'événement {EventId} pour l'organisation {OrgId}", eventId, orgId);
🔍 Tracing (OpenTelemetry)
Les traces sont automatiques pour EF Core et HTTP. Pour ajouter des traces manuelles dans un Handler critique :
using var activity = Monitoring.ActivitySource.StartActivity("CalculerItinéraire");
activity?.SetTag("user.id", userId);
// logique...
📈 Dashboard
Accédez au dashboard Aspire en local pour voir les traces, logs et métriques : https://localhost:18888 (port par défaut Aspire).

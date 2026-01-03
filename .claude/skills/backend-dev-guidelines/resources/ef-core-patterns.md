# Entity Framework Core & PostGIS Patterns

## 🗺️ PostGIS (Géolocalisation)
Nous utilisons `NetTopologySuite` pour les données géographiques.

```csharp
// Dans l'Entité
public Point Location { get; set; }

// Dans le DbContext
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<Event>()
        .Property(e => e.Location)
        .HasColumnType("geography (point)");
}
```
⚡ Performance
1. Lecture seule : Toujours utiliser .AsNoTracking() pour les Queries.
2. Pagination : Obligatoire pour toutes les listes.
3. N+1 : Utiliser .Include() ou Split Queries (.AsSplitQuery()) pour les collections chargées.
🔄 Migrations
• Créer : dotnet ef migrations add NomDeLaMigration -p Explore.Infrastructure -s Explore.Api
• Appliquer : dotnet ef database update
namespace Event.Domain.UnitTests.Interfaces;

using Explore.Domain;
using Explore.Domain.Interfaces;

public class InterfaceImplementationTests
{
    [Test]
    public async Task IAuditableEntity_Implementations_ExposeExpectedProperties()
    {
        var entityTypes = GetConcreteImplementations<IAuditableEntity>();

        await Assert.That(entityTypes.Length > 0).IsTrue();

        foreach (var entityType in entityTypes)
        {
            await Assert.That(HasProperty(entityType, nameof(IAuditableEntity.CreatedAt), typeof(DateTime))).IsTrue();
            await Assert.That(HasProperty(entityType, nameof(IAuditableEntity.CreatedBy), typeof(Guid?))).IsTrue();
            await Assert.That(HasProperty(entityType, nameof(IAuditableEntity.UpdatedAt), typeof(DateTime?))).IsTrue();
            await Assert.That(HasProperty(entityType, nameof(IAuditableEntity.UpdatedBy), typeof(Guid?))).IsTrue();
        }
    }

    [Test]
    public async Task ISoftDeletable_Implementations_ExposeExpectedProperties()
    {
        var entityTypes = GetConcreteImplementations<ISoftDeletable>();

        await Assert.That(entityTypes.Length > 0).IsTrue();

        foreach (var entityType in entityTypes)
        {
            await Assert.That(HasProperty(entityType, nameof(ISoftDeletable.IsDeleted), typeof(bool))).IsTrue();
            await Assert.That(HasProperty(entityType, nameof(ISoftDeletable.DeletedAt), typeof(DateTime?))).IsTrue();
            await Assert.That(HasProperty(entityType, nameof(ISoftDeletable.DeletedBy), typeof(Guid?))).IsTrue();
        }
    }

    [Test]
    public async Task ITenantEntity_Implementations_ExposeExpectedProperties()
    {
        var entityTypes = GetConcreteImplementations<ITenantEntity>();

        await Assert.That(entityTypes.Length > 0).IsTrue();

        foreach (var entityType in entityTypes)
        {
            await Assert.That(HasProperty(entityType, nameof(ITenantEntity.TenantId), typeof(Guid))).IsTrue();
        }
    }

    [Test]
    public async Task ReflectionScan_IncludesKnownInterfaceImplementations()
    {
        var auditableTypes = GetConcreteImplementations<IAuditableEntity>();
        var softDeletableTypes = GetConcreteImplementations<ISoftDeletable>();
        var tenantTypes = GetConcreteImplementations<ITenantEntity>();

        await Assert.That(auditableTypes.Contains(typeof(User))).IsTrue();
        await Assert.That(softDeletableTypes.Contains(typeof(User))).IsTrue();
        await Assert.That(tenantTypes.Contains(typeof(global::Explore.Domain.Event))).IsTrue();
    }

    private static Type[] GetConcreteImplementations<TInterface>()
    {
        return typeof(global::Explore.Domain.Event).Assembly
            .GetTypes()
            .Where(type => typeof(TInterface).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToArray();
    }

    private static bool HasProperty(Type type, string propertyName, Type expectedType)
    {
        var property = type.GetProperty(propertyName);
        return property is not null && property.PropertyType == expectedType;
    }
}

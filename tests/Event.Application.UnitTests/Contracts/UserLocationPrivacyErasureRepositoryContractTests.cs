// ABOUTME: Guards the Application persistence contract for User-owned Private Home erasure reads.
// ABOUTME: Requires a concrete entity collection and forbids IQueryable or DTO leakage.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using TUnit.Core;

namespace Event.Application.UnitTests.Contracts;

[Category("EventLocationPrivacy")]
public sealed class UserLocationPrivacyErasureRepositoryContractTests
{
    [Test]
    public async Task GlobalErasureRead_ReturnsLocationEntitiesInsteadOfQueryableOrDto()
    {
        var method = typeof(ILocationRepository).GetMethod(
            nameof(ILocationRepository.GetOwnedPrivateHomesForGlobalErasureAsync));

        await Assert.That(method).IsNotNull();
        await Assert.That(method!.ReturnType).IsEqualTo(typeof(Task<List<Location>>));
        await Assert.That(typeof(IQueryable).IsAssignableFrom(method.ReturnType)).IsFalse();
    }
}

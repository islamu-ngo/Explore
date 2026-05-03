// ABOUTME: Verifies custom-property projection inspection queries fail closed through resource authorization metadata.
// ABOUTME: Covers event and session projection read surfaces that expose status, dirty scopes, and projection rows.

using System.Reflection;
using Explore.Application.Authorization;
using Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;
using Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;

namespace Event.Application.UnitTests.Features.EventCustomPropertyProjections.Queries;

public class ProjectionQueryAuthorizationMetadataTests
{
    [Test]
    [Arguments(typeof(GetEventCustomPropertyProjectionStatusQuery))]
    [Arguments(typeof(GetEventSessionCustomPropertyProjectionStatusQuery))]
    [Arguments(typeof(GetCustomPropertyProjectionDirtyScopesQuery))]
    [Arguments(typeof(GetEventCustomPropertyProjectionsForEventQuery))]
    [Arguments(typeof(GetEventSessionCustomPropertyProjectionsForSessionQuery))]
    public async Task ProjectionInspectionQueriesRequireCustomPropertyProjectionViewPermission(Type queryType)
    {
        var attribute = queryType.GetCustomAttribute<AuthorizeResourceAttribute>();

        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Resource).IsEqualTo(ResourceKinds.CustomPropertyProjection);
        await Assert.That(attribute.Action).IsEqualTo(AuthorizationActions.CustomPropertyProjections.View);
        await Assert.That(typeof(ISecureRequest).IsAssignableFrom(queryType)).IsTrue();
    }
}

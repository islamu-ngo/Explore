// ABOUTME: Internal helper representing parent identifiers for group hierarchy mutations.
// ABOUTME: Resolves whether a group has a parent organization or group target.

using System;

namespace Explore.Application.Features.Groups;

internal readonly record struct GroupParentTarget(Guid? ParentOrganizationId, Guid? ParentGroupId);

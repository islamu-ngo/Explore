// ABOUTME: Defines the explicit local subject classifications accepted after verified ATProto OAuth.
// ABOUTME: Prevents application code from inferring Person, Organization, or Group from provider metadata.

namespace Explore.Application.Features.Authentication.Atproto.Models;

public enum AtprotoSubjectClassification
{
    Person = 1,
    Organization = 2,
    Group = 3
}

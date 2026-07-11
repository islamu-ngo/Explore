// ABOUTME: Defines TUnit category constants for infrastructure provider and runtime-adjacent tests.
// ABOUTME: Keeps focused Email and RabbitMQ test filters stable across infrastructure test classes.

namespace Explore.Infrastructure.Tests.Fixtures;

public static class InfrastructureTestCategories
{
    public const string Unit = "Unit";
    public const string Email = "Email";
    public const string RabbitMQ = "RabbitMQ";
    public const string Runtime = "Runtime";
    public const string Slow = "Slow";
    public const string Manual = "Manual";
}

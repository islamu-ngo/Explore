// ABOUTME: Unit tests for SecretRefreshOptions backoff calculations.
// Tests exponential backoff and jitter behavior.

using Explore.Secrets.Configuration;
using FluentAssertions;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Configuration;

public class SecretRefreshOptionsTests
{
    [Test]
    public void CalculateBackoffDelay_WhenZeroFailures_ShouldReturnZero()
    {
        // Arrange
        var options = new SecretRefreshOptions();

        // Act
        var delay = options.CalculateBackoffDelay(0);

        // Assert
        delay.Should().Be(TimeSpan.Zero);
    }

    [Test]
    public void CalculateBackoffDelay_WhenOneFailure_ShouldReturnBaseDelay()
    {
        // Arrange
        var options = new SecretRefreshOptions
        {
            BaseBackoffDelay = TimeSpan.FromSeconds(5),
            JitterFactor = 0 // No jitter for predictable test
        };

        // Act
        var delay = options.CalculateBackoffDelay(1);

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Test]
    public void CalculateBackoffDelay_ShouldDoubleWithEachFailure()
    {
        // Arrange
        var options = new SecretRefreshOptions
        {
            BaseBackoffDelay = TimeSpan.FromSeconds(5),
            MaxBackoffDelay = TimeSpan.FromMinutes(10),
            JitterFactor = 0 // No jitter for predictable test
        };

        // Act & Assert
        options.CalculateBackoffDelay(1).Should().Be(TimeSpan.FromSeconds(5));  // 5 * 2^0 = 5
        options.CalculateBackoffDelay(2).Should().Be(TimeSpan.FromSeconds(10)); // 5 * 2^1 = 10
        options.CalculateBackoffDelay(3).Should().Be(TimeSpan.FromSeconds(20)); // 5 * 2^2 = 20
        options.CalculateBackoffDelay(4).Should().Be(TimeSpan.FromSeconds(40)); // 5 * 2^3 = 40
    }

    [Test]
    public void CalculateBackoffDelay_ShouldCapAtMaxDelay()
    {
        // Arrange
        var options = new SecretRefreshOptions
        {
            BaseBackoffDelay = TimeSpan.FromSeconds(5),
            MaxBackoffDelay = TimeSpan.FromSeconds(30),
            JitterFactor = 0 // No jitter for predictable test
        };

        // Act
        var delay = options.CalculateBackoffDelay(10); // Would be 5 * 2^9 = 2560s without cap

        // Assert
        delay.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Test]
    public void CalculateBackoffDelay_WithJitter_ShouldAddRandomness()
    {
        // Arrange
        var options = new SecretRefreshOptions
        {
            BaseBackoffDelay = TimeSpan.FromSeconds(10),
            MaxBackoffDelay = TimeSpan.FromMinutes(5),
            JitterFactor = 0.1 // 10% jitter
        };

        // Act - Call multiple times to check randomness
        var delays = Enumerable.Range(0, 10)
            .Select(_ => options.CalculateBackoffDelay(1))
            .ToList();

        // Assert - Should all be between 10s and 11s (base + up to 10% jitter)
        foreach (var delay in delays)
        {
            delay.TotalSeconds.Should().BeGreaterThanOrEqualTo(10);
            delay.TotalSeconds.Should().BeLessThanOrEqualTo(11);
        }

        // At least some variance should exist (not all identical)
        delays.Distinct().Count().Should().BeGreaterThan(1);
    }

    [Test]
    public void AddJitter_ShouldAddRandomnessToInterval()
    {
        // Arrange
        var options = new SecretRefreshOptions
        {
            JitterFactor = 0.1 // 10% jitter
        };
        var baseInterval = TimeSpan.FromSeconds(100);

        // Act - Call multiple times
        var intervals = Enumerable.Range(0, 10)
            .Select(_ => options.AddJitter(baseInterval))
            .ToList();

        // Assert - Should all be between 100s and 110s
        foreach (var interval in intervals)
        {
            interval.TotalSeconds.Should().BeGreaterThanOrEqualTo(100);
            interval.TotalSeconds.Should().BeLessThanOrEqualTo(110);
        }
    }

    [Test]
    public void DefaultValues_ShouldBeReasonable()
    {
        // Arrange & Act
        var options = new SecretRefreshOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.RefreshInterval.Should().Be(TimeSpan.FromMinutes(5));
        options.InitialDelay.Should().Be(TimeSpan.FromSeconds(10));
        options.BaseBackoffDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.MaxBackoffDelay.Should().Be(TimeSpan.FromMinutes(5));
        options.JitterFactor.Should().Be(0.1);
        options.UnhealthyThreshold.Should().Be(3);
    }
}

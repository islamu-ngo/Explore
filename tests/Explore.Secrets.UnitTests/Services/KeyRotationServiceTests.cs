// ABOUTME: Unit tests for KeyRotationService.
// Tests re-encryption workflow, error handling, and progress tracking.

using Explore.Secrets.Abstractions;
using Explore.Secrets.Configuration;
using Explore.Secrets.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Services;

public class KeyRotationServiceTests : IDisposable
{
    // Valid 32-byte (256-bit) keys in base64
    private const string ValidKey1Base64 = "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=";
    private const string ValidKey2Base64 = "YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXowMTIzNDU=";

    private AesEncryptionService? _encryptionService;
    private KeyRotationService? _rotationService;

    private AesEncryptionService CreateEncryptionService(int currentKeyVersion = 2)
    {
        var options = new EncryptionOptions
        {
            CurrentKeyVersion = currentKeyVersion,
            KeyVersions = new Dictionary<int, string>
            {
                { 1, ValidKey1Base64 },
                { 2, ValidKey2Base64 }
            },
            AutoLoadFromEnvironment = false
        };

        _encryptionService = new AesEncryptionService(Options.Create(options));
        return _encryptionService;
    }

    private KeyRotationService CreateRotationService(IEncryptionService? encryptionService = null)
    {
        encryptionService ??= CreateEncryptionService();
        _rotationService = new KeyRotationService(encryptionService);
        return _rotationService;
    }

    // ==================== Constructor Tests ====================

    [Test]
    public void Constructor_WithNullEncryptionService_ShouldThrow()
    {
        // Act
        var act = () => new KeyRotationService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_WithValidEncryptionService_ShouldSucceed()
    {
        // Arrange & Act
        var service = CreateRotationService();

        // Assert
        service.Should().NotBeNull();
    }

    // ==================== ReEncryptAllAsync Tests ====================

    [Test]
    public async Task ReEncryptAllAsync_WithNoSettingsToRotate_ShouldReturnZeroResult()
    {
        // Arrange
        var service = CreateRotationService();
        var getSettings = Substitute.For<GetSettingsToRotateAsync>();
        getSettings.Invoke(Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<SettingToRotate>>([]));

        var updateSetting = Substitute.For<UpdateSettingAsync>();

        // Act
        var result = await service.ReEncryptAllAsync(getSettings, updateSetting);

        // Assert
        result.TotalSettings.Should().Be(0);
        result.ReEncryptedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task ReEncryptAllAsync_WithSettingsAtCurrentVersion_ShouldSkipAll()
    {
        // Arrange
        var encService = CreateEncryptionService(currentKeyVersion: 2);
        var service = CreateRotationService(encService);

        // Encrypt something with version 2 (current)
        var encrypted = encService.Encrypt("test");

        var getSettings = Substitute.For<GetSettingsToRotateAsync>();
        getSettings.Invoke(Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<SettingToRotate>>(
            [
                new SettingToRotate("key1", encrypted.Ciphertext, 2) // Already at version 2
            ]));

        var updateSetting = Substitute.For<UpdateSettingAsync>();

        // Act
        var result = await service.ReEncryptAllAsync(getSettings, updateSetting);

        // Assert
        result.TotalSettings.Should().Be(1);
        result.ReEncryptedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
        await updateSetting.DidNotReceive().Invoke(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>());
    }

    [Test]
    public async Task ReEncryptAllAsync_WithSettingsNeedingRotation_ShouldReEncrypt()
    {
        // Arrange
        // Create a service with only version 1, encrypt something
        var v1Options = new EncryptionOptions
        {
            CurrentKeyVersion = 1,
            KeyVersions = new Dictionary<int, string> { { 1, ValidKey1Base64 } },
            AutoLoadFromEnvironment = false
        };
        using var v1Service = new AesEncryptionService(Options.Create(v1Options));
        var encryptedV1 = v1Service.Encrypt("secret data");

        // Create a service with both versions, current = 2
        var encService = CreateEncryptionService(currentKeyVersion: 2);
        var service = CreateRotationService(encService);

        var getSettings = Substitute.For<GetSettingsToRotateAsync>();
        getSettings.Invoke(Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<SettingToRotate>>(
            [
                new SettingToRotate("key1", encryptedV1.Ciphertext, 1) // Old version
            ]));

        string? updatedCiphertext = null;
        int? updatedKeyVersion = null;

        var updateSetting = Substitute.For<UpdateSettingAsync>();
        updateSetting.Invoke(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(callInfo =>
            {
                updatedCiphertext = callInfo.ArgAt<string>(1);
                updatedKeyVersion = callInfo.ArgAt<int>(2);
                return Task.CompletedTask;
            });

        // Act
        var result = await service.ReEncryptAllAsync(getSettings, updateSetting);

        // Assert
        result.TotalSettings.Should().Be(1);
        result.ReEncryptedCount.Should().Be(1);
        result.SkippedCount.Should().Be(0);
        result.ErrorCount.Should().Be(0);

        updatedCiphertext.Should().NotBeNullOrEmpty();
        updatedKeyVersion.Should().Be(2);

        // Verify the new ciphertext can be decrypted
        var decrypted = encService.Decrypt(updatedCiphertext!, updatedKeyVersion!.Value);
        decrypted.Should().Be("secret data");
    }

    [Test]
    public async Task ReEncryptAllAsync_WithMissingOldKeyVersion_ShouldReportError()
    {
        // Arrange - Service only has version 2, not version 1
        var options = new EncryptionOptions
        {
            CurrentKeyVersion = 2,
            KeyVersions = new Dictionary<int, string> { { 2, ValidKey2Base64 } },
            AutoLoadFromEnvironment = false
        };
        using var encService = new AesEncryptionService(Options.Create(options));
        var service = CreateRotationService(encService);

        var getSettings = Substitute.For<GetSettingsToRotateAsync>();
        getSettings.Invoke(Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<SettingToRotate>>(
            [
                new SettingToRotate("key1", "some-encrypted-value", 1) // Old version not available
            ]));

        var updateSetting = Substitute.For<UpdateSettingAsync>();

        // Act
        var result = await service.ReEncryptAllAsync(getSettings, updateSetting);

        // Assert
        result.TotalSettings.Should().Be(1);
        result.ReEncryptedCount.Should().Be(0);
        result.SkippedCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Key.Should().Be("key1");
        result.Errors[0].OldKeyVersion.Should().Be(1);
        result.Errors[0].ErrorMessage.Should().Contain("Missing key version 1");
    }

    [Test]
    public async Task ReEncryptAllAsync_WithDecryptionError_ShouldReportError()
    {
        // Arrange
        var encService = CreateEncryptionService(currentKeyVersion: 2);
        var service = CreateRotationService(encService);

        var getSettings = Substitute.For<GetSettingsToRotateAsync>();
        getSettings.Invoke(Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<SettingToRotate>>(
            [
                new SettingToRotate("key1", "invalid-ciphertext", 1) // Invalid data
            ]));

        var updateSetting = Substitute.For<UpdateSettingAsync>();

        // Act
        var result = await service.ReEncryptAllAsync(getSettings, updateSetting);

        // Assert
        result.TotalSettings.Should().Be(1);
        result.ReEncryptedCount.Should().Be(0);
        result.ErrorCount.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Key.Should().Be("key1");
    }

    [Test]
    public async Task ReEncryptAllAsync_WithMultipleSettings_ShouldProcessAll()
    {
        // Arrange
        var v1Options = new EncryptionOptions
        {
            CurrentKeyVersion = 1,
            KeyVersions = new Dictionary<int, string> { { 1, ValidKey1Base64 } },
            AutoLoadFromEnvironment = false
        };
        using var v1Service = new AesEncryptionService(Options.Create(v1Options));
        var encrypted1 = v1Service.Encrypt("data1");
        var encrypted2 = v1Service.Encrypt("data2");
        var encrypted3 = v1Service.Encrypt("data3");

        var encService = CreateEncryptionService(currentKeyVersion: 2);
        var service = CreateRotationService(encService);

        var getSettings = Substitute.For<GetSettingsToRotateAsync>();
        getSettings.Invoke(Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<SettingToRotate>>(
            [
                new SettingToRotate("key1", encrypted1.Ciphertext, 1),
                new SettingToRotate("key2", encrypted2.Ciphertext, 1),
                new SettingToRotate("key3", encrypted3.Ciphertext, 1)
            ]));

        var updateCount = 0;
        var updateSetting = Substitute.For<UpdateSettingAsync>();
        updateSetting.Invoke(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(_ =>
            {
                updateCount++;
                return Task.CompletedTask;
            });

        // Act
        var result = await service.ReEncryptAllAsync(getSettings, updateSetting);

        // Assert
        result.TotalSettings.Should().Be(3);
        result.ReEncryptedCount.Should().Be(3);
        result.ErrorCount.Should().Be(0);
        updateCount.Should().Be(3);
    }

    [Test]
    public async Task ReEncryptAllAsync_WithProgress_ShouldReportProgress()
    {
        // Arrange
        var v1Options = new EncryptionOptions
        {
            CurrentKeyVersion = 1,
            KeyVersions = new Dictionary<int, string> { { 1, ValidKey1Base64 } },
            AutoLoadFromEnvironment = false
        };
        using var v1Service = new AesEncryptionService(Options.Create(v1Options));
        var encrypted1 = v1Service.Encrypt("data1");
        var encrypted2 = v1Service.Encrypt("data2");

        var encService = CreateEncryptionService(currentKeyVersion: 2);
        var service = CreateRotationService(encService);

        var getSettings = Substitute.For<GetSettingsToRotateAsync>();
        getSettings.Invoke(Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<SettingToRotate>>(
            [
                new SettingToRotate("key1", encrypted1.Ciphertext, 1),
                new SettingToRotate("key2", encrypted2.Ciphertext, 1)
            ]));

        var updateSetting = Substitute.For<UpdateSettingAsync>();
        updateSetting.Invoke(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(Task.CompletedTask);

        var progressReports = new List<KeyRotationProgress>();
        var progress = new SynchronousProgress<KeyRotationProgress>(p => progressReports.Add(p));

        // Act
        var result = await service.ReEncryptAllAsync(getSettings, updateSetting, progress);

        // Assert
        progressReports.Should().HaveCount(2);
        progressReports[0].Current.Should().Be(1);
        progressReports[0].Total.Should().Be(2);
        progressReports[0].Key.Should().Be("key1");
        progressReports[1].Current.Should().Be(2);
        progressReports[1].Total.Should().Be(2);
        progressReports[1].Key.Should().Be("key2");
    }

    [Test]
    public async Task ReEncryptAllAsync_WithCancellation_ShouldStop()
    {
        // Arrange
        var v1Options = new EncryptionOptions
        {
            CurrentKeyVersion = 1,
            KeyVersions = new Dictionary<int, string> { { 1, ValidKey1Base64 } },
            AutoLoadFromEnvironment = false
        };
        using var v1Service = new AesEncryptionService(Options.Create(v1Options));
        var encrypted1 = v1Service.Encrypt("data1");
        var encrypted2 = v1Service.Encrypt("data2");

        var encService = CreateEncryptionService(currentKeyVersion: 2);
        var service = CreateRotationService(encService);

        var getSettings = Substitute.For<GetSettingsToRotateAsync>();
        getSettings.Invoke(Arg.Any<int>())
            .Returns(Task.FromResult<IReadOnlyList<SettingToRotate>>(
            [
                new SettingToRotate("key1", encrypted1.Ciphertext, 1),
                new SettingToRotate("key2", encrypted2.Ciphertext, 1)
            ]));

        using var cts = new CancellationTokenSource();

        var updateCount = 0;
        var updateSetting = Substitute.For<UpdateSettingAsync>();
        updateSetting.Invoke(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>())
            .Returns(_ =>
            {
                updateCount++;
                cts.Cancel(); // Cancel after first update
                return Task.CompletedTask;
            });

        // Act
        var act = async () => await service.ReEncryptAllAsync(
            getSettings,
            updateSetting,
            cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        updateCount.Should().Be(1);
    }

    // ==================== ReEncryptSingle Tests ====================

    [Test]
    public void ReEncryptSingle_WithValidInput_ShouldReturnNewEncryption()
    {
        // Arrange
        var v1Options = new EncryptionOptions
        {
            CurrentKeyVersion = 1,
            KeyVersions = new Dictionary<int, string> { { 1, ValidKey1Base64 } },
            AutoLoadFromEnvironment = false
        };
        using var v1Service = new AesEncryptionService(Options.Create(v1Options));
        var encryptedV1 = v1Service.Encrypt("secret data");

        var encService = CreateEncryptionService(currentKeyVersion: 2);
        var service = CreateRotationService(encService);

        // Act
        var result = service.ReEncryptSingle(encryptedV1.Ciphertext, encryptedV1.KeyVersion);

        // Assert
        result.Should().NotBeNull();
        result.KeyVersion.Should().Be(2);
        result.Ciphertext.Should().NotBe(encryptedV1.Ciphertext);

        // Verify decryption works
        var decrypted = encService.Decrypt(result.Ciphertext, result.KeyVersion);
        decrypted.Should().Be("secret data");
    }

    [Test]
    public void ReEncryptSingle_WithMissingKeyVersion_ShouldThrow()
    {
        // Arrange - Service only has version 2
        var options = new EncryptionOptions
        {
            CurrentKeyVersion = 2,
            KeyVersions = new Dictionary<int, string> { { 2, ValidKey2Base64 } },
            AutoLoadFromEnvironment = false
        };
        using var encService = new AesEncryptionService(Options.Create(options));
        var service = CreateRotationService(encService);

        // Act
        var act = () => service.ReEncryptSingle("some-ciphertext", 1);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot re-encrypt*key version 1 not available*");
    }

    [Test]
    public void ReEncryptSingle_WithEmptyCiphertext_ShouldThrow()
    {
        // Arrange
        var service = CreateRotationService();

        // Act
        var act = () => service.ReEncryptSingle("", 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void ReEncryptSingle_WithNullCiphertext_ShouldThrow()
    {
        // Arrange
        var service = CreateRotationService();

        // Act
        var act = () => service.ReEncryptSingle(null!, 1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    public void Dispose()
    {
        _encryptionService?.Dispose();
    }

    /// <summary>
    /// Synchronous IProgress implementation that avoids the SynchronizationContext
    /// race conditions inherent in <see cref="Progress{T}"/> during unit tests.
    /// </summary>
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}

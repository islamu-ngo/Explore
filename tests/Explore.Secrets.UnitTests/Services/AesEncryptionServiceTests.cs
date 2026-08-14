// ABOUTME: Unit tests for AesEncryptionService.
// ABOUTME: Tests encryption/decryption, key versioning, error handling, and secure disposal.

using Explore.Secrets.Configuration;
using Explore.Secrets.Services;
using Microsoft.Extensions.Options;
using TUnit.Core;

namespace Explore.Secrets.UnitTests.Services;

public class AesEncryptionServiceTests : IDisposable
{
    // Valid 32-byte (256-bit) key in base64
    private const string ValidKey1Base64 = "MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE="; // "01234567890123456789012345678901"
    private const string ValidKey2Base64 = "YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXowMTIzNDU="; // "abcdefghijklmnopqrstuvwxyz012345"

    private readonly byte[] _validKey1;
    private readonly byte[] _validKey2;
    private AesEncryptionService? _service;

    public AesEncryptionServiceTests()
    {
        _validKey1 = Convert.FromBase64String(ValidKey1Base64);
        _validKey2 = Convert.FromBase64String(ValidKey2Base64);
    }

    private AesEncryptionService CreateService(
        Dictionary<int, string>? keyVersions = null,
        int currentKeyVersion = 1)
    {
        var options = new EncryptionOptions
        {
            CurrentKeyVersion = currentKeyVersion,
            KeyVersions = keyVersions ?? new Dictionary<int, string>
            {
                { 1, ValidKey1Base64 }
            },
            AutoLoadFromEnvironment = false
        };

        _service = new AesEncryptionService(Options.Create(options));
        return _service;
    }

    private AesEncryptionService CreateServiceWithByteKeys(
        Dictionary<int, byte[]>? keyVersions = null,
        int currentKeyVersion = 1)
    {
        keyVersions ??= new Dictionary<int, byte[]>
        {
            { 1, _validKey1 }
        };

        _service = new AesEncryptionService(keyVersions, currentKeyVersion);
        return _service;
    }

    // ==================== Constructor Tests ====================

    [Test]
    public async Task Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange & Act
        var service = CreateService();

        // Assert
        await Assert.That(service.CurrentKeyVersion).IsEqualTo(1);
        await Assert.That(service.AvailableKeyVersions).Contains(1);
    }

    [Test]
    public async Task Constructor_WithMultipleKeyVersions_ShouldLoadAll()
    {
        // Arrange & Act
        var service = CreateService(new Dictionary<int, string>
        {
            { 1, ValidKey1Base64 },
            { 2, ValidKey2Base64 }
        }, currentKeyVersion: 2);

        // Assert
        await Assert.That(service.CurrentKeyVersion).IsEqualTo(2);
        await Assert.That(service.AvailableKeyVersions).Count().IsEqualTo(2);
        await Assert.That(service.AvailableKeyVersions).Contains(1);
        await Assert.That(service.AvailableKeyVersions).Contains(2);
    }

    [Test]
    public async Task Constructor_WithNoKeys_ShouldThrow()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            CurrentKeyVersion = 1,
            KeyVersions = new Dictionary<int, string>(),
            AutoLoadFromEnvironment = false
        };

        // Act
        var act = () => new AesEncryptionService(Options.Create(options));

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("No encryption keys configured");
    }

    [Test]
    public async Task Constructor_WithMissingCurrentKeyVersion_ShouldThrow()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            CurrentKeyVersion = 2, // Version 2 doesn't exist
            KeyVersions = new Dictionary<int, string>
            {
                { 1, ValidKey1Base64 }
            },
            AutoLoadFromEnvironment = false
        };

        // Act
        var act = () => new AesEncryptionService(Options.Create(options));

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Current key version 2 not found");
    }

    [Test]
    public async Task Constructor_WithInvalidBase64Key_ShouldThrow()
    {
        // Arrange
        var options = new EncryptionOptions
        {
            CurrentKeyVersion = 1,
            KeyVersions = new Dictionary<int, string>
            {
                { 1, "not-valid-base64!!!" }
            },
            AutoLoadFromEnvironment = false
        };

        // Act
        var act = () => new AesEncryptionService(Options.Create(options));

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Key version 1 is not valid base64");
    }

    [Test]
    public async Task Constructor_WithWrongKeyLength_ShouldThrow()
    {
        // Arrange - 16 bytes instead of 32
        var shortKey = Convert.ToBase64String(new byte[16]);
        var options = new EncryptionOptions
        {
            CurrentKeyVersion = 1,
            KeyVersions = new Dictionary<int, string>
            {
                { 1, shortKey }
            },
            AutoLoadFromEnvironment = false
        };

        // Act
        var act = () => new AesEncryptionService(Options.Create(options));

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Key version 1 must be exactly 32 bytes");
    }

    [Test]
    public async Task Constructor_WithByteKeys_EmptyDictionary_ShouldThrow()
    {
        // Arrange & Act
        var act = () => new AesEncryptionService(new Dictionary<int, byte[]>(), 1);

        // Assert
        await Assert.That(act).Throws<ArgumentException>()
            .WithMessageContaining("At least one key version is required");
    }

    [Test]
    public async Task Constructor_WithByteKeys_WrongKeyLength_ShouldThrow()
    {
        // Arrange
        var keyVersions = new Dictionary<int, byte[]>
        {
            { 1, new byte[16] } // Wrong length
        };

        // Act
        var act = () => new AesEncryptionService(keyVersions, 1);

        // Assert
        await Assert.That(act).Throws<ArgumentException>()
            .WithMessageContaining("Key version 1 must be exactly 32 bytes");
    }

    // ==================== Encryption Tests ====================

    [Test]
    public async Task Encrypt_WithValidPlaintext_ShouldReturnEncryptedResult()
    {
        // Arrange
        var service = CreateService();
        var plaintext = "Hello, World!";

        // Act
        var result = service.Encrypt(plaintext);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Ciphertext).IsNotNullOrEmpty();
        await Assert.That(result.KeyVersion).IsEqualTo(1);
        await Assert.That(result.Ciphertext).IsNotEqualTo(plaintext);
    }

    [Test]
    public async Task Encrypt_SameTextTwice_ShouldProduceDifferentCiphertext()
    {
        // Arrange
        var service = CreateService();
        var plaintext = "Hello, World!";

        // Act
        var result1 = service.Encrypt(plaintext);
        var result2 = service.Encrypt(plaintext);

        // Assert - due to random nonce, ciphertext should differ
        await Assert.That(result1.Ciphertext).IsNotEqualTo(result2.Ciphertext);
    }

    [Test]
    public async Task Encrypt_WithEmptyString_ShouldSucceed()
    {
        // Arrange
        var service = CreateService();
        var plaintext = "";

        // Act
        var result = service.Encrypt(plaintext);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Ciphertext).IsNotNullOrEmpty();
    }

    [Test]
    public async Task Encrypt_WithLargeText_ShouldSucceed()
    {
        // Arrange
        var service = CreateService();
        var plaintext = new string('A', 100000); // 100KB

        // Act
        var result = service.Encrypt(plaintext);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Ciphertext).IsNotNullOrEmpty();
    }

    [Test]
    public async Task Encrypt_WithUnicodeText_ShouldSucceed()
    {
        // Arrange
        var service = CreateService();
        var plaintext = "Hello 世界! Привет мир! مرحبا بالعالم";

        // Act
        var result = service.Encrypt(plaintext);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Ciphertext).IsNotNullOrEmpty();
    }

    [Test]
    public async Task Encrypt_WithNullPlaintext_ShouldThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.Encrypt(null!);

        // Assert
        await Assert.That(act).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Encrypt_AfterDispose_ShouldThrow()
    {
        // Arrange
        var service = CreateService();
        service.Dispose();

        // Act
        var act = () => service.Encrypt("test");

        // Assert
        await Assert.That(act).Throws<ObjectDisposedException>();
    }

    // ==================== Decryption Tests ====================

    [Test]
    public async Task Decrypt_WithValidCiphertext_ShouldReturnOriginalPlaintext()
    {
        // Arrange
        var service = CreateService();
        var originalText = "Hello, World!";
        var encrypted = service.Encrypt(originalText);

        // Act
        var decrypted = service.Decrypt(encrypted.Ciphertext, encrypted.KeyVersion);

        // Assert
        await Assert.That(decrypted).IsEqualTo(originalText);
    }

    [Test]
    public async Task Decrypt_WithEmptyStringEncrypted_ShouldReturnEmptyString()
    {
        // Arrange
        var service = CreateService();
        var originalText = "";
        var encrypted = service.Encrypt(originalText);

        // Act
        var decrypted = service.Decrypt(encrypted.Ciphertext, encrypted.KeyVersion);

        // Assert
        await Assert.That(decrypted).IsEmpty();
    }

    [Test]
    public async Task Decrypt_WithUnicodeText_ShouldReturnOriginal()
    {
        // Arrange
        var service = CreateService();
        var originalText = "Hello 世界! Привет мир! مرحبا بالعالم";
        var encrypted = service.Encrypt(originalText);

        // Act
        var decrypted = service.Decrypt(encrypted.Ciphertext, encrypted.KeyVersion);

        // Assert
        await Assert.That(decrypted).IsEqualTo(originalText);
    }

    [Test]
    public async Task Decrypt_WithWrongKeyVersion_ShouldThrow()
    {
        // Arrange
        var service = CreateService();
        var encrypted = service.Encrypt("test");

        // Act
        var act = () => service.Decrypt(encrypted.Ciphertext, 999);

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Key version 999 not found");
    }

    [Test]
    public async Task Decrypt_WithInvalidBase64_ShouldThrow()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.Decrypt("not-valid-base64!!!", 1);

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Invalid ciphertext format");
    }

    [Test]
    public async Task Decrypt_WithTruncatedCiphertext_ShouldThrow()
    {
        // Arrange
        var service = CreateService();
        var shortCiphertext = Convert.ToBase64String(new byte[10]); // Too short

        // Act
        var act = () => service.Decrypt(shortCiphertext, 1);

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("Invalid ciphertext: too short");
    }

    [Test]
    public async Task Decrypt_WithTamperedCiphertext_ShouldThrow()
    {
        // Arrange
        var service = CreateService();
        var encrypted = service.Encrypt("test");

        // Tamper with the ciphertext
        var bytes = Convert.FromBase64String(encrypted.Ciphertext);
        bytes[20] ^= 0xFF; // Flip some bits
        var tampered = Convert.ToBase64String(bytes);

        // Act
        var act = () => service.Decrypt(tampered, encrypted.KeyVersion);

        // Assert
        await Assert.That(act).Throws<InvalidOperationException>()
            .WithMessageContaining("authentication tag mismatch");
    }

    [Test]
    public async Task Decrypt_AfterDispose_ShouldThrow()
    {
        // Arrange
        var service = CreateService();
        var encrypted = service.Encrypt("test");
        service.Dispose();

        // Act
        var act = () => service.Decrypt(encrypted.Ciphertext, encrypted.KeyVersion);

        // Assert
        await Assert.That(act).Throws<ObjectDisposedException>();
    }

    // ==================== Key Version Tests ====================

    [Test]
    public async Task HasKeyVersion_WithExistingVersion_ShouldReturnTrue()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.That(service.HasKeyVersion(1)).IsTrue();
    }

    [Test]
    public async Task HasKeyVersion_WithNonExistingVersion_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.That(service.HasKeyVersion(999)).IsFalse();
    }

    [Test]
    public async Task HasKeyVersion_AfterDispose_ShouldThrow()
    {
        // Arrange
        var service = CreateService();
        service.Dispose();

        // Act
        var act = () => service.HasKeyVersion(1);

        // Assert
        await Assert.That(act).Throws<ObjectDisposedException>();
    }

    // ==================== Multi-Version Tests ====================

    [Test]
    public async Task MultiVersion_EncryptWithCurrentVersion_ShouldUseCurrentVersion()
    {
        // Arrange
        var service = CreateService(new Dictionary<int, string>
        {
            { 1, ValidKey1Base64 },
            { 2, ValidKey2Base64 }
        }, currentKeyVersion: 2);

        // Act
        var result = service.Encrypt("test");

        // Assert
        await Assert.That(result.KeyVersion).IsEqualTo(2);
    }

    [Test]
    public async Task MultiVersion_DecryptWithOldVersion_ShouldSucceed()
    {
        // Arrange
        var service1 = CreateService(new Dictionary<int, string>
        {
            { 1, ValidKey1Base64 }
        }, currentKeyVersion: 1);

        var encrypted = service1.Encrypt("test");
        service1.Dispose();

        // Create new service with both old and new key
        var service2 = CreateService(new Dictionary<int, string>
        {
            { 1, ValidKey1Base64 },
            { 2, ValidKey2Base64 }
        }, currentKeyVersion: 2);

        // Act
        var decrypted = service2.Decrypt(encrypted.Ciphertext, encrypted.KeyVersion);

        // Assert
        await Assert.That(decrypted).IsEqualTo("test");
    }

    [Test]
    public async Task MultiVersion_ReEncrypt_ShouldWorkCorrectly()
    {
        // Arrange - Encrypt with version 1
        var service1 = CreateService(new Dictionary<int, string>
        {
            { 1, ValidKey1Base64 }
        }, currentKeyVersion: 1);

        var originalText = "secret data";
        var encryptedV1 = service1.Encrypt(originalText);
        service1.Dispose();

        // Re-encrypt with version 2
        var service2 = CreateService(new Dictionary<int, string>
        {
            { 1, ValidKey1Base64 },
            { 2, ValidKey2Base64 }
        }, currentKeyVersion: 2);

        // Decrypt old, re-encrypt with new
        var decrypted = service2.Decrypt(encryptedV1.Ciphertext, encryptedV1.KeyVersion);
        var encryptedV2 = service2.Encrypt(decrypted);

        // Assert
        await Assert.That(encryptedV2.KeyVersion).IsEqualTo(2);

        // Verify can decrypt with only version 2
        service2.Dispose();
        var service3 = CreateService(new Dictionary<int, string>
        {
            { 2, ValidKey2Base64 }
        }, currentKeyVersion: 2);

        var finalDecrypted = service3.Decrypt(encryptedV2.Ciphertext, encryptedV2.KeyVersion);
        await Assert.That(finalDecrypted).IsEqualTo(originalText);
    }

    // ==================== Disposal Tests ====================

    [Test]
    public async Task Dispose_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - Should not throw
        service.Dispose();
        service.Dispose();
        service.Dispose();
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}

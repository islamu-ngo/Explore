// ABOUTME: Shared Blazor upload UX policy for image file hints, safe messages, and log-safe buckets.
// ABOUTME: Sanitizes browser-provided filename metadata before image upload services send it to the BFF/API.

using System.Text;

namespace Explore.Blazor.Client.Services;

public static class ImageUploadClientPolicy
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public const long DefaultMaxImageFileSizeBytes = 5 * 1024 * 1024;
    public const string DefaultAcceptedImageFormats = ".jpg,.jpeg,.png,.gif,.webp";

    public const string UnsupportedImageTypeMessage = "Select a JPG, PNG, GIF, or WebP image.";
    public const string OversizedImageMessagePrefix = "Image must be";
    public const string ReadFailureMessage = "Failed to read the selected image. Try another file.";
    public const string PreviewFailureMessage = "Failed to generate an image preview.";
    public const string ProcessingFailureMessage = "An error occurred while processing the image.";
    public const string GenericUploadFailureMessage = "Image upload failed. Try again or choose another image.";
    public const string NoImageDataMessage = "No image data was provided.";
    public const string UploadSessionUnavailableMessage = "Failed to get an upload session. Please check your authentication and try again.";
    public const string UploadProxyFailureMessage = "Failed to upload image to storage. Please check your connection and try again.";
    public const string MetadataFailureMessage = "Failed to save image metadata. Please try again.";
    public const string MetadataBuildFailureMessage = "Failed to build storage metadata for uploaded image.";
    public const string StorageUploadCompletedWithoutMetadataMessage = "Storage upload completed without metadata.";
    public const string DirectUploadBrowserUnavailableMessage = "Browser uploads require a server-issued upload session.";

    private static readonly string[] DefaultAllowedImageContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    ];

    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp"
    };

    private static readonly HashSet<string> SafeImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp"
    };

    private static readonly HashSet<string> UserSafeUploadMessages = new(StringComparer.Ordinal)
    {
        GenericUploadFailureMessage,
        NoImageDataMessage,
        UploadSessionUnavailableMessage,
        UploadProxyFailureMessage,
        MetadataFailureMessage,
        MetadataBuildFailureMessage,
        StorageUploadCompletedWithoutMetadataMessage,
        DirectUploadBrowserUnavailableMessage
    };

    public static string[] AllowedImageContentTypes => DefaultAllowedImageContentTypes.ToArray();

    public static string? DetectImageContentType(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (content.StartsWith(PngSignature))
        {
            return "image/png";
        }

        if (content.StartsWith("GIF87a"u8) || content.StartsWith("GIF89a"u8))
        {
            return "image/gif";
        }

        return content.Length >= 12 &&
               content[..4].SequenceEqual("RIFF"u8) &&
               content[8..12].SequenceEqual("WEBP"u8)
            ? "image/webp"
            : null;
    }

    public static bool IsAllowedImageContentType(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType)
            && DefaultAllowedImageContentTypes.Contains(contentType.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static string FormatMaxFileSizeMessage(long maxFileSize)
    {
        return $"{OversizedImageMessagePrefix} {FormatBytes(maxFileSize)} or smaller.";
    }

    public static string FormatBytes(long bytes)
    {
        const double oneMiB = 1024d * 1024d;
        return bytes >= oneMiB
            ? $"{bytes / oneMiB:0.#} MB"
            : $"{bytes} bytes";
    }

    public static string BuildSafeFileName(string? browserFileName, string? contentType)
    {
        var extension = ResolveSafeImageExtension(browserFileName, contentType);
        var baseName = BuildSafeFileNameStem(browserFileName);

        return $"{baseName}{extension}";
    }

    public static string ResolveSafeImageExtension(string? browserFileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType)
            && ExtensionByContentType.TryGetValue(contentType.Trim(), out var mappedExtension))
        {
            return mappedExtension;
        }

        var lastSegment = GetLastPathSegment(browserFileName);
        var extension = Path.GetExtension(lastSegment);
        return SafeImageExtensions.Contains(extension) ? extension.ToLowerInvariant() : ".jpg";
    }

    public static string GetSizeBucket(long sizeBytes)
    {
        return sizeBytes switch
        {
            <= 0 => "empty",
            <= 1024 * 1024 => "0-1MB",
            <= DefaultMaxImageFileSizeBytes => "1-5MB",
            <= 10 * 1024 * 1024 => "5-10MB",
            _ => ">10MB"
        };
    }

    public static string GetContentTypeBucket(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "missing";
        }

        var trimmed = contentType.Trim();
        if (IsAllowedImageContentType(trimmed))
        {
            return "allowed-image";
        }

        return trimmed.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            ? "other-image"
            : "other";
    }

    public static string GetFailureType(Exception exception)
    {
        return exception.GetType().Name;
    }

    public static string ToUserSafeUploadError(string? message)
    {
        var trimmed = message?.Trim();
        return !string.IsNullOrEmpty(trimmed) && UserSafeUploadMessages.Contains(trimmed)
            ? trimmed
            : GenericUploadFailureMessage;
    }

    private static string BuildSafeFileNameStem(string? browserFileName)
    {
        var lastSegment = GetLastPathSegment(browserFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(lastSegment);
        var builder = new StringBuilder(nameWithoutExtension.Length);
        var previousWasSeparator = false;

        foreach (var character in nameWithoutExtension.Normalize(NormalizationForm.FormD))
        {
            if (IsAsciiLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (character is '.' or '_' or '-')
            {
                AppendSeparator(builder, character, ref previousWasSeparator);
            }
            else if (!previousWasSeparator)
            {
                AppendSeparator(builder, '-', ref previousWasSeparator);
            }
        }

        var safeName = builder.ToString().Trim('.', '_', '-');
        if (safeName.Length > 64)
        {
            safeName = safeName[..64].Trim('.', '_', '-');
        }

        return string.IsNullOrWhiteSpace(safeName) ? "image" : safeName;
    }

    private static string GetLastPathSegment(string? browserFileName)
    {
        if (string.IsNullOrWhiteSpace(browserFileName))
        {
            return "image";
        }

        var normalized = browserFileName.Trim().Replace('\\', '/');
        var lastSeparator = normalized.LastIndexOf('/');
        return lastSeparator >= 0 ? normalized[(lastSeparator + 1)..] : normalized;
    }

    private static void AppendSeparator(StringBuilder builder, char separator, ref bool previousWasSeparator)
    {
        if (builder.Length == 0)
        {
            previousWasSeparator = true;
            return;
        }

        if (previousWasSeparator)
        {
            return;
        }

        builder.Append(separator);
        previousWasSeparator = true;
    }

    private static bool IsAsciiLetterOrDigit(char character)
    {
        return character is >= 'a' and <= 'z'
            || character is >= 'A' and <= 'Z'
            || character is >= '0' and <= '9';
    }
}

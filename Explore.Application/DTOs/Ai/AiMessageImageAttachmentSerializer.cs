// ABOUTME: Normalizes AI assistant image attachment payloads for storage and provider use.
// ABOUTME: Keeps base64 image data private on read DTOs while preserving it for queued AI runs.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure.Ai;

namespace Explore.Application.DTOs.Ai;

internal static class AiMessageImageAttachmentSerializer
{
    public const int MaxImageCount = 4;
    public const int MaxImageBytes = 5 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string? SerializeForStorage(IEnumerable<AiMessageImageInputDto>? images)
    {
        var attachments = Normalize(images).ToList();
        return attachments.Count == 0
            ? null
            : JsonSerializer.Serialize(attachments, JsonOptions);
    }

    public static IReadOnlyList<AiChatImage> DeserializeForProvider(string? imageAttachmentsJson) =>
        Deserialize(imageAttachmentsJson)
            .Where(image => !string.IsNullOrWhiteSpace(image.MediaType) && !string.IsNullOrWhiteSpace(image.Data))
            .Select(image => new AiChatImage(image.MediaType.Trim(), image.Data.Trim()))
            .ToList();

    public static IReadOnlyList<AiMessageImageDto> DeserializeMetadata(string? imageAttachmentsJson) =>
        Deserialize(imageAttachmentsJson)
            .Where(image => !string.IsNullOrWhiteSpace(image.MediaType))
            .Select(image => new AiMessageImageDto
            {
                MediaType = image.MediaType.Trim(),
                FileName = string.IsNullOrWhiteSpace(image.FileName) ? null : image.FileName.Trim(),
                SizeBytes = image.SizeBytes
            })
            .ToList();

    public static IReadOnlyList<StoredAiMessageImageAttachmentDto> DeserializeForStorage(string? imageAttachmentsJson) =>
        Deserialize(imageAttachmentsJson)
            .Where(image => !string.IsNullOrWhiteSpace(image.MediaType) && !string.IsNullOrWhiteSpace(image.Data))
            .Select(image => new StoredAiMessageImageAttachmentDto(
                image.MediaType.Trim(),
                image.Data.Trim(),
                string.IsNullOrWhiteSpace(image.FileName) ? null : image.FileName.Trim(),
                image.SizeBytes))
            .ToList();

    private static IEnumerable<StoredAiMessageImageDto> Normalize(IEnumerable<AiMessageImageInputDto>? images)
    {
        if (images is null)
        {
            yield break;
        }

        foreach (var image in images)
        {
            if (image is null)
            {
                continue;
            }

            var mediaType = image.MediaType?.Trim() ?? string.Empty;
            var data = image.Data?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(mediaType) || string.IsNullOrWhiteSpace(data))
            {
                continue;
            }

            yield return new StoredAiMessageImageDto
            {
                MediaType = mediaType,
                Data = data,
                FileName = string.IsNullOrWhiteSpace(image.FileName) ? null : image.FileName.Trim(),
                SizeBytes = image.SizeBytes
            };
        }
    }

    private static IReadOnlyList<StoredAiMessageImageDto> Deserialize(string? imageAttachmentsJson)
    {
        if (string.IsNullOrWhiteSpace(imageAttachmentsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<StoredAiMessageImageDto>>(imageAttachmentsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed class StoredAiMessageImageDto
    {
        public string MediaType { get; init; } = string.Empty;
        public string Data { get; init; } = string.Empty;
        public string? FileName { get; init; }
        public long? SizeBytes { get; init; }
    }
}

internal sealed record StoredAiMessageImageAttachmentDto(
    string MediaType,
    string Data,
    string? FileName,
    long? SizeBytes);

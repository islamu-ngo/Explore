// ABOUTME: DTO for instance-level S3 storage settings managed via admin UI.
// ABOUTME: Represents the 8 S3 configuration fields stored in SystemSetting records.

namespace Explore.Application.DTOs.Onboarding;

public class InstanceStorageSettingsDto
{
    public string S3Endpoint { get; set; } = string.Empty;
    public string S3PublicEndpoint { get; set; } = string.Empty;
    public string S3BucketName { get; set; } = string.Empty;
    public string S3AccessKeyId { get; set; } = string.Empty;
    public string S3SecretAccessKey { get; set; } = string.Empty;
    public string S3Region { get; set; } = string.Empty;
    public bool S3ForcePathStyle { get; set; } = true;
    public int S3UploadUrlExpirationMinutes { get; set; } = 60;
}

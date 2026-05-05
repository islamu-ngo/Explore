// ABOUTME: Usage report projection for instance-admin metadata reporting.
// ABOUTME: Aggregates per-key request counts and credit usage without exposing secret material.

namespace Explore.Application.DTOs.ExternalApiKey;

public class ExternalApiKeyUsageReportDto
{
    public Guid ApiKeyId { get; set; }
    public required string ApiKeyName { get; set; }
    public Guid? TenantId { get; set; }
    public int ExternalApiKeyOwnerTypeId { get; set; }
    public required string ExternalApiKeyOwnerTypeCode { get; set; }
    public required string ExternalApiKeyOwnerTypeName { get; set; }
    public Guid OwnerId { get; set; }
    public long TotalRequestCount { get; set; }
    public int TotalCreditsUsed { get; set; }
    public int CreditLimit { get; set; }
}

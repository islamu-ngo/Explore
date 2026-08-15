// ABOUTME: Non-secret promotion-code digest configuration bound from application settings.
// ABOUTME: Carries only the active HMAC key version; secret bytes resolve through SecretBinding.

namespace Explore.Application.Configuration;

public sealed class PromotionCodeLookupOptions
{
    public const string SectionName = "Promotions:CodeLookup";

    public int ActiveKeyVersion { get; set; } = 1;
}

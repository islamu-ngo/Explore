// ABOUTME: Defines the stable instance-setting resource identifier for SMTP processor controls.
// ABOUTME: Keeps global CQRS authorization and HAL permission checks on one instance-admin seam.

namespace Explore.Application.Features.EmailDispatch;

public static class EmailDispatchProcessorControl
{
    public const string SettingKey = "email-dispatch.processor";
    public const int MaximumGlobalRateLimitPerMinute = 100000;
}

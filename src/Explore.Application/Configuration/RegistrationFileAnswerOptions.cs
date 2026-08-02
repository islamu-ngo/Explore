// ABOUTME: Controls whether deployments may publish registration forms containing File fields.
// ABOUTME: Defaults off while the quarantined file-answer pipeline has no malware scanner integration.

namespace Explore.Application.Configuration;

public sealed class RegistrationFileAnswerOptions
{
    public const string SectionName = "Registration:FileAnswers";

    public bool Enabled { get; set; }
}

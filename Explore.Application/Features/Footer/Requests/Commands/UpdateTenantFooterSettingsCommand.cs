// ABOUTME: Command to update tenant-level footer scalar settings (template, description, social links, copyright).
// ABOUTME: Respects instance-level lock flags — locked settings are silently skipped.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Footer.Requests.Commands;

public class UpdateTenantFooterSettingsCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid UserId { get; set; }
    public bool? Enabled { get; set; }
    public string? Template { get; set; }
    public bool? ShowDescription { get; set; }
    public string? DescriptionText { get; set; }
    public bool? ShowSocialLinks { get; set; }
    public string? SocialLinksJson { get; set; }
    public string? CopyrightText { get; set; }
    public bool? ShowCookieSettingsLink { get; set; }
}

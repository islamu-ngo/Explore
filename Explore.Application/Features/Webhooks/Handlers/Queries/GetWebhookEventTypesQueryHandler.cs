// ABOUTME: Query handler that maps canonical webhook event descriptors into API catalog DTOs.
// ABOUTME: Keeps catalog reads provider-neutral and generated from the same registry used by delivery providers.

using System.Text.Json;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.DTOs.Webhooks;
using Explore.Application.Features.Webhooks.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Queries;

public sealed class GetWebhookEventTypesQueryHandler(
    IWebhookEventTypeRegistry eventTypeRegistry,
    IWebhookEventSchemaProvider schemaProvider)
    : IRequestHandler<GetWebhookEventTypesQuery, IReadOnlyList<WebhookEventTypeDto>>
{
    public Task<IReadOnlyList<WebhookEventTypeDto>> Handle(
        GetWebhookEventTypesQuery request,
        CancellationToken cancellationToken)
    {
        var eventTypes = eventTypeRegistry
            .GetAll()
            .OrderBy(descriptor => descriptor.GroupName, StringComparer.Ordinal)
            .ThenBy(descriptor => descriptor.Name, StringComparer.Ordinal)
            .Select(Map)
            .ToList();

        return Task.FromResult<IReadOnlyList<WebhookEventTypeDto>>(eventTypes);
    }

    private WebhookEventTypeDto Map(WebhookEventTypeDescriptor descriptor) =>
        new()
        {
            Name = descriptor.Name,
            GroupName = descriptor.GroupName,
            Description = descriptor.Description,
            SchemaVersion = descriptor.SchemaVersion,
            IsPublic = descriptor.IsPublic,
            IsEnabled = descriptor.IsEnabled,
            PayloadRetentionDays = descriptor.PayloadRetentionDays,
            SchemaJson = schemaProvider.CreateSchemaJson(descriptor),
            ExamplePayloadJson = schemaProvider.CreateExamplePayloadJson(descriptor),
            DataFields = descriptor.DataFields.Select(MapField).ToList()
        };

    private static WebhookEventDataFieldDto MapField(WebhookEventDataFieldDescriptor field) =>
        new()
        {
            Name = field.Name,
            JsonType = field.JsonType,
            Description = field.Description,
            ExampleJson = JsonSerializer.Serialize(field.Example),
            Required = field.Required
        };
}

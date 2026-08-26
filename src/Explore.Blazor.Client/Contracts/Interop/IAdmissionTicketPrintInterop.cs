// ABOUTME: Defines the browser print action used by sensitive admission ticket surfaces.
// ABOUTME: Keeps Razor components free of direct JavaScript runtime calls.

namespace Explore.Blazor.Client.Contracts.Interop;

public interface IAdmissionTicketPrintInterop : IAsyncDisposable
{
    ValueTask PrintAsync(CancellationToken cancellationToken = default);
}

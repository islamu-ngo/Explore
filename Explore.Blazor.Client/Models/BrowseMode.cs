// ABOUTME: Enum defining browsing modes for the event list: infinite scroll or traditional pagination.
// ABOUTME: URL params (?page=N) trigger Pagination mode; default is InfiniteScroll for existing behavior.

namespace Explore.Blazor.Client.Models;

public enum BrowseMode
{
    InfiniteScroll,
    Pagination
}

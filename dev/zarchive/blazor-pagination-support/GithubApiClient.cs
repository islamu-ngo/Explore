public async IAsyncEnumerable<Repo> QueryReposAsync(RepoQuery query)
{
    int reposPerPage = 10;
    var page = 1;
    
    while (true) {
        var url =
            $"https://api.github.com/search/repositories?" +
            $"{(query.HasQuery ? query.QueryClause + "&" : string.Empty)}" +
            $"{(query.HasSortOrder ? $"{query.SortClause}&{query.OrderClause}&" : string.Empty)}" +
            $"page={page}&per_page={reposPerPage}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "RepoSearchAgent");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (!json.RootElement.TryGetProperty("items", out var items)) break;
        
        foreach (var item in items.EnumerateArray()) {
            var owner = item.GetProperty("owner").GetProperty("login").GetString() ?? string.Empty;
            var name = item.GetProperty("name").GetString() ?? string.Empty;
            var description = item.GetProperty("description").GetString() ?? string.Empty;
            var stars = item.GetProperty("stargazers_count").GetInt32();
        }
    }
}
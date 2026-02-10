public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
{
    AnsiConsole.MarkupLine("[green]Executing 'repos' command...[/]");

    if (!string.IsNullOrEmpty(settings.Token))
    {
        AnsiConsole.MarkupLine("[yellow]Using provided GitHub token for authentication... [/]");
        _githubApiClient.SetToken(settings.Token);
    }

    var table = new Table();
    new List<string> { "#", "Repo Name", "Owner", "Language", "Last Modified", "Stars", "Forks" }.ForEach(header => table.AddColumn($"[bold]{header}[/]"));

    AnsiConsole.MarkupLine("[yellow]Fetching repositories from GitHub...[/]");
    var repoCount = 0;

    var query1 = new RepoQuery()
        .And(RepoFilter.Language("C#"))
        .And(RepoFilter.Stars(11))
        .SortByDescending(RepoSort.Stars);
    
    var query2 = new RepoQuery()
        .And(RepoFilter.DescriptionPart("maui"))
        .SortByDescending(RepoSort.Forks);

    await foreach (var repo in _githubApiClient.QueryReposAsync(query1)
    {
        table.AddRow(
            (repoCount + 1).ToString(), repo.Name, repo.Owner, repo.Language, repo.LastModified.ToString(), repo.StarsCount.ToString(), repo.ForksCount.ToString());
        if (++repoCount >= settings.Rows) break;
    }
    
    AnsiConsole.WriteLine();
    AnsiConsole.Write(table);

    return 0;
}
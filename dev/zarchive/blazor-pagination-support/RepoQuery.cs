public class RepoQuery : IQuerySpecification<Repo>
{
    private RepoSort? _sort = null;
    private bool _ascending = false;
    private RepoFilter[] _filters = Array.Empty<RepoFilter>();

    public RepoQuery() { }

    private RepoQuery(RepoFilter[] filters, RepoSort? sort, bool ascending)
    {
        _filters = filters;
        _sort = sort;
        _ascending = ascending;
    }

    public RepoQuery And(RepoFilter other) => new([.. _filters, other], _sort, _ascending);
    public RepoQuery SortBy(RepoSort sort) => new(_filters, sort, true);
    public RepoQuery SortByDescending(RepoSort sort) => new(_filters, sort, false);

    internal bool HasQuery => _filters.Lenght > 0;
    internal string QueryClause => _filters.Length == 0 ? string.Empty : $"q={string.Join("+", _filters.Select(f => f.Filter))}";

    internal bool HasSortOrder => _sort != null;
    internal string SortClause => _sort != null ? string.Empty : $"sort={Uri.EscapeDataString(_sort.Field)}";
    internal string OrderClause => _ascending ? "order=asc" : "order=desc";
}

public interface IQuerySpecification<T>
{
    IQuerySpecification<T> And(IFilterSpecification<T> other);
    IQuerySpecification<T> Or(IFilterSpecification<T> other);
    IQuerySpecification<T> Not();

    IQuerySpecification<T> SortBy(ISortSpecification<T> sort);
    IQuerySpecification<T> SortByDescending(ISortSpecification<T> sort);
}

public interface IFilterSpecification<T>
{

}

public interface ISortSpecification<T>
{

}

interface IQueryable<T>
{
    IQueryable<T> Where(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
    IOrderedQueryable<T> OrderBy<TKey>(System.Linq.Expressions.Expression<Func<T, TKey>> keySelector);
    IOrderedQueryable<T> OrderByDescending<TKey>(System.Linq.Expressions.Expression<Func<T, TKey>> keySelector);
}

public class RepoFilter
{
    internal string Filter { get; }
    private RepoFilter(string filter) => Filter = filter;

    public static RepoFilter Language(string language) => new($"language:{Uri.EscapeDataString(language)}");
    public static RepoFilter Stars(int minStars) => new($"stars:>{minStars - 1}");
    public static RepoFilter NamePart(string name) => new($"{Uri.EscapeDataString(name)} in:name");
    public static RepoFilter DescriptionPart(string name) => new($"{Uri.EscapeDataString(name)} in:description");

}

public class RepoSort
{
    internal string Field { get; }

    private RepoSort(string field) => Field = field;

    public static RepoSort Stars => new RepoSort("stars");
    public static RepoSort Forks => new RepoSort("forks");
    public static RepoSort LastModified => new RepoSort("updated");

}

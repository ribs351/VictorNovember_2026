namespace VictorNovember.Services.BraveSearch.Models;

public class PaginatedView<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageSize { get; }

    public PaginatedView(IEnumerable<T> items, int pageSize = 5)
    {
        Items = items.ToList();
        PageSize = pageSize;
    }

    public int TotalPages =>
        (int)Math.Ceiling(Items.Count / (double)PageSize);

    public IReadOnlyList<T> GetPage(int page)
    {
        return Items
            .Skip(page * PageSize)
            .Take(PageSize)
            .ToList();
    }
}

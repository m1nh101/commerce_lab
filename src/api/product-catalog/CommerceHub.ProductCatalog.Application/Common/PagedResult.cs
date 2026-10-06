namespace CommerceHub.ProductCatalog.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int Limit)
{
    public int TotalPages => Limit == 0 ? 0 : (int)Math.Ceiling(Total / (double)Limit);
}

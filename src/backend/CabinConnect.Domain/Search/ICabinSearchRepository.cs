using CabinConnect.Domain.Cabins;

namespace CabinConnect.Domain.Search;

public interface ICabinSearchRepository
{
    Task<(IReadOnlyList<Cabin> Page, int TotalCount)> SearchAvailablePageAsync(
        CabinSearchQuery query, CancellationToken ct = default);
}

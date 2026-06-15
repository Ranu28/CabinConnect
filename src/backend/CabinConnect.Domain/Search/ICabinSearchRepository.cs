using CabinConnect.Domain.Cabins;

namespace CabinConnect.Domain.Search;

public interface ICabinSearchRepository
{
    Task<(IReadOnlyList<Cabin> Page, int TotalCount)> SearchAvailablePageAsync(
        CabinSearchQuery query, CancellationToken ct = default);

    /// <summary>Returns a published Cabin by ID, or null if it doesn't exist or is unpublished.</summary>
    Task<Cabin?> GetCabinByIdAsync(Guid cabinId, CancellationToken ct = default);
}

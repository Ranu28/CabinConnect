namespace CabinConnect.Domain.Search;

public interface ICabinSearchRepository
{
    Task<IReadOnlyList<CabinWithAvailabilityData>> GetPublishedCabinsWithAvailabilityDataAsync(
        DateOnly checkIn, DateOnly checkOut, CancellationToken ct = default);
}

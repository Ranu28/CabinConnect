using CabinConnect.Domain.Rates;

namespace CabinConnect.Domain.Bookings;

public sealed class BookingService
{
    private readonly IBookingRepository _repository;

    public BookingService(IBookingRepository repository)
    {
        _repository = repository;
    }

    public Task<(Booking Booking, RateBreakdown PriceBreakdown)> ConfirmHoldAsync(
        Guid holdId, Guid guestId, CancellationToken ct = default)
        => _repository.ConfirmHoldAsync(holdId, guestId, ct);
}

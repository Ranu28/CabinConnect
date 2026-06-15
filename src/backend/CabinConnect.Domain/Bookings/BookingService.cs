using CabinConnect.Domain.Common;
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

    public Task CancelBookingAsync(Guid bookingId, Guid guestId, CancellationToken ct = default)
        => _repository.CancelBookingAsync(bookingId, guestId, ct);

    public Task<PagedResult<GuestBookingItem>> GetGuestBookingsAsync(
        Guid guestId, int page, int pageSize, CancellationToken ct = default)
        => _repository.GetGuestBookingsAsync(guestId, page, pageSize, ct);

    public Task<GuestBookingItem?> GetGuestBookingByIdAsync(
        Guid bookingId, Guid guestId, CancellationToken ct = default)
        => _repository.GetGuestBookingByIdAsync(bookingId, guestId, ct);

    public Task<PagedResult<HostBookingItem>> GetHostBookingsAsync(
        Guid hostId, BookingStatus? status, int page, int pageSize, CancellationToken ct = default)
        => _repository.GetHostBookingsAsync(hostId, status, page, pageSize, ct);
}

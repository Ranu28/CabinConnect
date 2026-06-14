# Prompt Log — BKF-002: Confirm Booking

**Date:** 2026-06-14
**Unit:** [BKF-002 Confirm Booking](../ops/build/units/bkf-002-confirm-booking.md)
**Bolt:** BOLT-002
**Output Format:** Full implementation (SQL migration + C# domain/infra/API + xUnit tests)

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CabinConnect backend — BKF-002 Confirm Booking; C# .NET 10 / Dapper / Npgsql / PostgreSQL |
| **Constraints** | CLAUDE.md rules; EC-002 expiry check at confirm; EC-006 price frozen server-side; EC-007 ownership before status; EC-012 atomic Hold+Booking transaction; no `.Result`/`.Wait()` |
| **Acceptance Criteria** | 8 ACs from bkf-002-confirm-booking.md |
| **Output Format** | Full implementation: SQL migration, domain model extensions, repository interface, infrastructure, service, controller, tests |

---

## Files Generated

| File | Purpose |
|---|---|
| `infra/database/migrations/002_bookings_add_currency.sql` | Adds currency column to bookings table |
| `src/backend/CabinConnect.Domain/Bookings/Booking.cs` (updated) | Added GuestId, HoldId, TotalPrice, Currency |
| `src/backend/CabinConnect.Domain/Bookings/IBookingRepository.cs` | Repository interface returning (Booking, RateBreakdown) |
| `src/backend/CabinConnect.Domain/Bookings/BookingService.cs` | Thin service delegating to IBookingRepository |
| `src/backend/CabinConnect.Domain/Holds/HoldNotFoundException.cs` | AC-8 → 404 |
| `src/backend/CabinConnect.Domain/Holds/HoldExpiredException.cs` | AC-2 → 409 HOLD_EXPIRED |
| `src/backend/CabinConnect.Domain/Holds/HoldNotActiveException.cs` | AC-3 → 409 HOLD_NOT_ACTIVE |
| `src/backend/CabinConnect.Domain/Holds/HoldOwnershipException.cs` | AC-4 → 403 |
| `src/backend/CabinConnect.Infrastructure/Bookings/BookingRepository.cs` | Atomic tx: FOR UPDATE OF h + JOIN cabins, validation, NightlyRateCalculator, INSERT booking, UPDATE hold=Consumed |
| `src/backend/CabinConnect.Api/Models/ConfirmBookingRequest.cs` | Request DTO |
| `src/backend/CabinConnect.Api/Controllers/BookingsController.cs` | POST /api/bookings; [Authorize]; exception→HTTP mapping; GroupBreakdown helper |
| `src/backend/CabinConnect.Api/Program.cs` (updated) | Registered IBookingRepository + BookingService |
| `tests/CabinConnect.Domain.Tests/Bookings/BookingServiceTests.cs` | 5 service tests |
| `tests/CabinConnect.Api.Tests/Controllers/BookingsControllerTests.cs` | 6 controller integration tests |

---

## AC Coverage

| AC | Test | Status |
|---|---|---|
| AC-1 Booking confirmed, 201 + bookingId + totalPrice + status=Confirmed | `ConfirmBooking_ValidHold_Returns201WithBookingDetails` | Covered |
| AC-2 Expired Hold → 409 HOLD_EXPIRED (EC-002) | `ConfirmBooking_ExpiredHold_Returns409HoldExpired` | Covered |
| AC-3 Consumed/Cancelled → 409 HOLD_NOT_ACTIVE | `ConfirmBooking_InactiveHold_Returns409HoldNotActive` | Covered |
| AC-4 Wrong Guest → 403 (EC-007) | `ConfirmBooking_WrongGuest_Returns403` | Covered |
| AC-5 No auth → 401 | `ConfirmBooking_NoAuth_Returns401` | Covered |
| AC-6 Price frozen server-side (EC-006) | NightlyRateCalculator called in BookingRepository; client price ignored | Covered by design |
| AC-7 Atomicity (EC-012) | FOR UPDATE + rollback on exception | Requires integration test |
| AC-8 Hold not found → 404 | `ConfirmBooking_HoldNotFound_Returns404` | Covered |

---

## Key Design Decisions

- **`FOR UPDATE OF h`**: locks only the holds row in the JOIN query, not the cabin row. Prevents two concurrent calls confirming the same Hold.
- **Ownership checked before status** (AC-4 before AC-3/AC-2): avoids leaking status information about holds owned by other guests (EC-007).
- **`BookingRepository` calls `NightlyRateCalculator`**: infrastructure references domain (allowed — domain is innermost layer). Keeps the entire price-freeze operation atomic within the single transaction.
- **`priceBreakdown` not persisted**: computed from `RateBreakdown` after INSERT; only `total_price` is stored per EC-006.

## Known Gaps

- **AC-7 integration test**: atomicity (rollback on error) requires a real DB; flagged as retro watch item.
- **AC-6 test**: price freeze is enforced by design (client input rejected) but no test explicitly sends a fake price and verifies it is ignored. Edge case for the retro.

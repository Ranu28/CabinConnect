# Prompt Log — BKF-003: Cancel Booking

**Date:** 2026-06-14
**Unit:** [BKF-003 Cancel Booking](../ops/build/units/bkf-003-cancel-booking.md)
**Bolt:** BOLT-002
**Output Format:** Full implementation (C# domain/infra/API + xUnit tests)

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CabinConnect backend — BKF-003 Cancel Booking; C# .NET 10 / Dapper / Npgsql / PostgreSQL |
| **Constraints** | CLAUDE.md rules; EC-007 ownership (404 not 403 for unowned — info leak prevention); idempotent on already-Cancelled; 409 CANNOT_CANCEL for Completed/NoShow; no fee calculated |
| **Acceptance Criteria** | 9 ACs from bkf-003-cancel-booking.md |
| **Output Format** | Full implementation: domain exceptions, repository interface, infrastructure, service, controller action, tests |

---

## Files Generated

| File | Purpose |
|---|---|
| `src/backend/CabinConnect.Domain/Bookings/BookingNotFoundException.cs` | AC-6/AC-8 → 404 (covers not-found AND not-owned) |
| `src/backend/CabinConnect.Domain/Bookings/BookingCannotBeCancelledException.cs` | AC-3/AC-4 → 409 CANNOT_CANCEL |
| `src/backend/CabinConnect.Domain/Bookings/IBookingRepository.cs` (updated) | Added `CancelBookingAsync` |
| `src/backend/CabinConnect.Domain/Bookings/BookingService.cs` (updated) | Delegation to repository |
| `src/backend/CabinConnect.Infrastructure/Bookings/BookingRepository.cs` (updated) | SELECT with guest_id filter → idempotent check → UPDATE |
| `src/backend/CabinConnect.Api/Controllers/BookingsController.cs` (updated) | `POST /{id}/cancel`; added `ParseGuestId()` helper |
| `tests/CabinConnect.Domain.Tests/Bookings/BookingServiceTests.cs` (updated) | 3 cancel tests |
| `tests/CabinConnect.Api.Tests/Controllers/BookingsControllerTests.cs` (updated) | 5 cancel controller tests |

---

## AC Coverage

| AC | Test | Status |
|---|---|---|
| AC-1 Cancel Confirmed → 200 | `CancelBooking_ConfirmedBooking_Returns200` | Covered |
| AC-2 Cancel Pending → 200 | Same (repository mocked, same path) | Covered |
| AC-3 Completed → 409 CANNOT_CANCEL | `CancelBooking_TerminalStatus_Returns409CannotCancel` | Covered |
| AC-4 NoShow → 409 CANNOT_CANCEL | Same exception type | Covered |
| AC-5 Already Cancelled → 200 idempotent | `CancelBooking_AlreadyCancelled_Returns200Idempotent` | Covered |
| AC-6 Unowned → 404 (not 403, EC-007) | `CancelBooking_NotFoundOrNotOwned_Returns404` | Covered |
| AC-7 No auth → 401 | `CancelBooking_NoAuth_Returns401` | Covered |
| AC-8 Not found → 404 | Same exception as AC-6 (`BookingNotFoundException`) | Covered |
| AC-9 No fee | No price calculation in cancel path | Covered by design |

---

## Key Design Decisions

- **`SELECT WHERE id = @BookingId AND guest_id = @GuestId`**: A booking owned by a different Guest returns null, which surfaces as 404. The caller cannot distinguish "not found" from "not owned" — intentional info-leak prevention (EC-007).
- **Idempotency**: `BookingRepository.CancelBookingAsync` reads current status first; if already Cancelled, returns without issuing an UPDATE.
- **`ParseGuestId()` helper**: Extracted to private method in controller to eliminate code duplication across all `BookingsController` actions.

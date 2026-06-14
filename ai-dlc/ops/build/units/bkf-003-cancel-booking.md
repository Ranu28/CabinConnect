# Unit: Cancel Booking

**ID:** BKF-003
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Backend — Domain + API
**Priority:** High
**Date:** 2026-06-14

---

## Purpose

Allow an authenticated Guest to cancel their own Confirmed or Pending Booking. Cancellation is always free and immediate. Terminal-state bookings (Completed, NoShow) cannot be cancelled.

## Acceptance Criteria

**AC-1 — Confirmed Booking cancelled:**
- **Given** an authenticated Guest and a Confirmed Booking they own
- **When** `POST /api/bookings/{id}/cancel` is called
- **Then** the Booking `status` transitions to `Cancelled`; HTTP 200

**AC-2 — Pending Booking cancelled:**
- **Given** an authenticated Guest and a Pending Booking they own
- **When** `POST /api/bookings/{id}/cancel` is called
- **Then** the Booking `status` transitions to `Cancelled`; HTTP 200

**AC-3 — Completed Booking cannot be cancelled:**
- **Given** a Booking with `status=Completed`
- **When** `POST /api/bookings/{id}/cancel` is called
- **Then** HTTP 409 with code `CANNOT_CANCEL`

**AC-4 — NoShow Booking cannot be cancelled:**
- **Given** a Booking with `status=NoShow`
- **When** `POST /api/bookings/{id}/cancel` is called
- **Then** HTTP 409 with code `CANNOT_CANCEL`

**AC-5 — Already-Cancelled Booking is idempotent:**
- **Given** a Booking already with `status=Cancelled`
- **When** `POST /api/bookings/{id}/cancel` is called
- **Then** HTTP 200 (no-op; cancelling an already-cancelled booking is safe)

**AC-6 — Booking owned by different Guest rejected (EC-007):**
- **Given** the Booking's `guestId` does not match the authenticated user
- **When** `POST /api/bookings/{id}/cancel` is called
- **Then** HTTP 403

**AC-7 — Unauthenticated request rejected:**
- **Given** no valid JWT
- **When** `POST /api/bookings/{id}/cancel` is called
- **Then** HTTP 401

**AC-8 — Booking not found:**
- **Given** a `bookingId` that does not exist
- **When** `POST /api/bookings/{id}/cancel` is called
- **Then** HTTP 404

**AC-9 — Cancellation is free:**
- **Given** any cancellable Booking
- **When** cancellation completes
- **Then** no fee is calculated, charged, or stored; `totalPrice` on the Booking is unchanged

## Edge Cases Addressed

- EC-007 — Ownership validated server-side before any mutation (AC-6); RLS also restricts at DB level

## Dependencies

- BKF-002 — Bookings must exist to be cancelled

## API Contract

**Endpoint:** `POST /api/bookings/{id}/cancel`
**Auth:** Required (Bearer JWT)

**Request Body:** None

**Response (200):**
```typescript
{
  data: {
    bookingId: string;
    status:    "Cancelled";
  };
  error: null;
}
```

**Error Codes:**
| Code | HTTP | Condition |
|---|---|---|
| `CANNOT_CANCEL` | 409 | Booking is Completed or NoShow |
| `NOT_FOUND` | 404 | bookingId does not exist or not owned by Guest |

## Notes

- Using `POST /api/bookings/{id}/cancel` (verb-action) rather than `DELETE /api/bookings/{id}` because DELETE would imply destroying the record; we want the Booking to remain in the audit trail with status=Cancelled.
- The 404 response for "not owned by Guest" (vs 403) is intentional — revealing that a booking exists is an information leak. RLS at the DB level enforces this automatically: a Guest querying a booking they don't own gets 0 rows, which the API surfaces as 404.

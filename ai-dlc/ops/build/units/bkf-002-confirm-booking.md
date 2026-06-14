# Unit: Confirm Booking from Hold

**ID:** BKF-002
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Backend — Domain + API
**Priority:** High
**Date:** 2026-06-14

---

## Purpose

Allow an authenticated Guest to confirm a Booking using an Active Hold. Calculates and freezes the Total Price at the moment of confirmation, transitions the Hold to Consumed, and creates a Confirmed Booking — all within a single database transaction to prevent double-booking.

## Acceptance Criteria

**AC-1 — Booking confirmed from Active Hold:**
- **Given** an authenticated Guest and an Active, non-expired Hold they own
- **When** `POST /api/bookings` is called with the `holdId`
- **Then** a Booking is created with `status=Confirmed`, `totalPrice` frozen at current nightly rates, and the Hold transitions to `Consumed`; response includes `bookingId`, `totalPrice`, and `checkIn`/`checkOut`

**AC-2 — Expired Hold rejected (EC-002):**
- **Given** a Hold whose `expiresAt` has passed (even if status is still `Active`)
- **When** `POST /api/bookings` is called
- **Then** HTTP 409 with code `HOLD_EXPIRED`

**AC-3 — Consumed or Cancelled Hold rejected:**
- **Given** a Hold with `status=Consumed` or `status=Cancelled`
- **When** `POST /api/bookings` is called
- **Then** HTTP 409 with code `HOLD_NOT_ACTIVE`

**AC-4 — Hold owned by different Guest rejected (EC-007):**
- **Given** the Hold's `guestId` does not match the authenticated user
- **When** `POST /api/bookings` is called
- **Then** HTTP 403

**AC-5 — Unauthenticated request rejected:**
- **Given** no valid JWT
- **When** `POST /api/bookings` is called
- **Then** HTTP 401

**AC-6 — Total price frozen at confirmation (EC-006):**
- **Given** a valid Hold and Active seasonal rates for the cabin
- **When** the Booking is confirmed
- **Then** `totalPrice` on the Booking equals the output of `NightlyRateCalculator` using rates current at confirmation time; the client-supplied price (if any) is ignored

**AC-7 — Atomicity: Hold consumed and Booking created together (EC-012):**
- **Given** a valid Hold
- **When** the Booking creation step fails (e.g. DB error after Hold is locked)
- **Then** the Hold remains `Active` (transaction rolled back) and no Booking is created

**AC-8 — Hold not found:**
- **Given** a `holdId` that does not exist
- **When** `POST /api/bookings` is called
- **Then** HTTP 404

## Edge Cases Addressed

- EC-001 — Hold is locked with `SELECT FOR UPDATE` within the transaction, preventing concurrent confirmation of the same Hold
- EC-002 — `expiresAt` checked server-side at confirmation time regardless of Hold status field (AC-2)
- EC-006 — Total price calculated server-side from current rates, never from client input (AC-6)
- EC-007 — Guest ID validated against Hold's `guestId` (AC-4)
- EC-012 — Hold consumption and Booking creation in one atomic transaction (AC-7)

## Dependencies

- BKF-001 — Hold must exist before it can be confirmed
- CSA-001 — `NightlyRateCalculator` used to compute and freeze Total Price

## API Contract

**Endpoint:** `POST /api/bookings`
**Auth:** Required (Bearer JWT)

**Request Body:**
```typescript
{
  holdId: string; // UUID of an Active Hold
}
```

**Response (201):**
```typescript
{
  data: {
    bookingId:        string;  // UUID
    holdId:           string;
    cabinId:          string;
    checkIn:          string;  // ISO date
    checkOut:         string;  // ISO date
    totalPrice:       number;
    currency:         string;
    status:           "Confirmed";
    priceBreakdown:   RateLineItem[];
  };
  error: null;
}
```

**Error Codes:**
| Code | HTTP | Condition |
|---|---|---|
| `HOLD_EXPIRED` | 409 | Hold's expiresAt has passed |
| `HOLD_NOT_ACTIVE` | 409 | Hold status is Consumed or Cancelled |
| `NOT_FOUND` | 404 | holdId does not exist |

## Notes

- The Booking is created with `status=Confirmed` directly — there is no `Pending` step since payment processing (Stripe) is out of scope for this intent. The `payment_intent_id` column is left null.
- `priceBreakdown` in the response is computed for display only; only `totalPrice` is persisted on the Booking.
- The transaction sequence: `BEGIN` → `SELECT hold FOR UPDATE` → validate → fetch seasonal rates → calculate price → `INSERT booking` → `UPDATE hold SET status='Consumed'` → `COMMIT`.

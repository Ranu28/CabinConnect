# Unit: Place Hold

**ID:** BKF-001
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Backend — Domain + API
**Priority:** High
**Date:** 2026-06-14

---

## Purpose

Allow an authenticated Guest to place a temporary Hold on an available Cabin for a requested date range, preventing other Guests from booking those dates for 15 minutes while checkout proceeds. Also updates the availability query (CSA-002) to exclude cabins with Active Holds.

## Acceptance Criteria

**AC-1 — Hold created for available cabin:**
- **Given** an authenticated Guest and a published Cabin with no overlapping Confirmed/Pending Bookings, Active Holds, or Blackout Dates
- **When** `POST /api/holds` is called with valid `cabinId`, `checkIn`, `checkOut`
- **Then** a Hold is created with `status=Active` and `expiresAt=now+15min`; response includes `holdId` and `expiresAt`

**AC-2 — Cabin unavailable due to existing Booking:**
- **Given** the Cabin has a Confirmed or Pending Booking overlapping the requested dates
- **When** `POST /api/holds` is called
- **Then** HTTP 409 with code `CABIN_UNAVAILABLE`

**AC-3 — Cabin unavailable due to Active Hold:**
- **Given** the Cabin already has an Active Hold overlapping the requested dates
- **When** `POST /api/holds` is called
- **Then** HTTP 409 with code `CABIN_UNAVAILABLE`

**AC-4 — Cabin unavailable due to Blackout Date (EC-004):**
- **Given** the Cabin has a Blackout Date overlapping the requested dates
- **When** `POST /api/holds` is called
- **Then** HTTP 409 with code `CABIN_UNAVAILABLE`

**AC-5 — Previous Active Hold auto-cancelled (EC-011):**
- **Given** the Guest already has an Active Hold on any Cabin
- **When** `POST /api/holds` is called for a different (or the same) Cabin
- **Then** the previous Hold is transitioned to `Cancelled` and the new Hold is created

**AC-6 — Unauthenticated request rejected:**
- **Given** no valid JWT in the request
- **When** `POST /api/holds` is called
- **Then** HTTP 401

**AC-7 — Invalid date range rejected (EC-009):**
- **Given** `checkOut` ≤ `checkIn`
- **When** `POST /api/holds` is called
- **Then** HTTP 400 with code `INVALID_DATE_RANGE`

**AC-8 — Zero-night stay rejected (EC-010):**
- **Given** `checkIn` equals `checkOut`
- **When** `POST /api/holds` is called
- **Then** HTTP 400 with code `ZERO_NIGHT_STAY`

**AC-9 — Unpublished or non-existent cabin rejected:**
- **Given** `cabinId` does not exist or the Cabin is not published
- **When** `POST /api/holds` is called
- **Then** HTTP 404

**AC-10 — Availability query excludes Active Holds:**
- **Given** a Cabin has an Active Hold overlapping a date range
- **When** `GET /api/cabins/search` is called with that date range
- **Then** that Cabin is excluded from search results

## Edge Cases Addressed

- EC-001 — Concurrent Hold creation checked within a DB transaction with row-level lock
- EC-003 — Dates stored and compared as UTC date-only (AC-7, AC-8)
- EC-004 — Blackout Dates checked in availability validation (AC-4)
- EC-009 — Check-out before check-in returns 400 (AC-7)
- EC-010 — Zero-night stay returns 400 (AC-8)
- EC-011 — Existing Active Hold auto-cancelled before new Hold is created (AC-5)

## Dependencies

- None for the Hold API itself
- CSA-002 availability query (CabinSearchRepository) must be updated to add `NOT EXISTS` for Active Holds — this is part of this unit's definition of done

## DB Schema Changes

New table:
```sql
create table holds (
    id         uuid        primary key default gen_random_uuid(),
    cabin_id   uuid        not null references cabins (id),
    guest_id   uuid        not null references auth.users (id),
    check_in   date        not null,
    check_out  date        not null,
    expires_at timestamptz not null,
    status     text        not null default 'Active',
    created_at timestamptz not null default now(),
    constraint hold_valid_dates  check (check_out > check_in),
    constraint hold_valid_status check (status in ('Active', 'Consumed', 'Cancelled'))
);

create index idx_holds_availability
    on holds (cabin_id, check_in, check_out, status, expires_at);

create index idx_holds_guest
    on holds (guest_id, status);

alter table holds enable row level security;

-- Guests can only read/write their own Holds
create policy "holds_guest_select" on holds for select using (auth.uid() = guest_id);
create policy "holds_guest_insert" on holds for insert with check (auth.uid() = guest_id);
create policy "holds_guest_update" on holds for update using (auth.uid() = guest_id);
```

`bookings` table additions (run alongside this unit):
```sql
alter table bookings add column if not exists hold_id uuid references holds (id);
alter table bookings add column if not exists payment_intent_id text;
```

## API Contract

**Endpoint:** `POST /api/holds`
**Auth:** Required (Bearer JWT)

**Request Body:**
```typescript
{
  cabinId:  string; // UUID
  checkIn:  string; // ISO date e.g. "2026-08-01"
  checkOut: string; // ISO date e.g. "2026-08-05"
}
```

**Response (201):**
```typescript
{
  data: {
    holdId:    string;      // UUID
    cabinId:   string;
    checkIn:   string;
    checkOut:  string;
    expiresAt: string;      // ISO 8601 timestamp
    status:    "Active";
  };
  error: null;
}
```

**Response (4xx):**
```typescript
{
  data: null;
  error: { code: string; message: string; }
}
```

**Error Codes:**
| Code | HTTP | Condition |
|---|---|---|
| `CABIN_UNAVAILABLE` | 409 | Overlapping Booking, Hold, or Blackout Date |
| `INVALID_DATE_RANGE` | 400 | checkOut ≤ checkIn |
| `ZERO_NIGHT_STAY` | 400 | checkIn === checkOut |
| `NOT_FOUND` | 404 | cabinId not found or not published |

## Notes

- Hold creation must be atomic: cancel previous Hold + insert new Hold in one transaction.
- Availability check for the new Hold must also happen within the same transaction with a `SELECT FOR UPDATE` on the Cabin row (or equivalent advisory lock) to prevent EC-001.
- The `expiresAt` is always `UTC now + 15 minutes` — never client-supplied.
- The CSA-002 update adds: `AND NOT EXISTS (SELECT 1 FROM holds h WHERE h.cabin_id = c.id AND h.check_in < @CheckOut AND h.check_out > @CheckIn AND h.status = 'Active' AND h.expires_at > now())` to the `available` CTE.

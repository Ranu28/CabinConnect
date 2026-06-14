# Unit: Host Booking Management

**ID:** BKF-009
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Backend + Frontend
**Priority:** Low
**Date:** 2026-06-14

---

## Purpose

Provides a paginated API endpoint and a frontend page that shows a Host all Bookings across their Cabins, filterable by status. Hosts cannot cancel or reject Bookings in this unit — view only.

## Acceptance Criteria

### Backend — `GET /api/host/bookings`

**AC-1 — Returns all Bookings for Host's Cabins:**
- **Given** an authenticated Host with Cabins that have Bookings
- **When** `GET /api/host/bookings` is called
- **Then** all Bookings for Cabins where `cabins.host_id = authenticated user ID` are returned, sorted by `checkIn` descending, with `cabinName`, `checkIn`, `checkOut`, `totalPrice`, `currency`, `status`, and `guestId`

**AC-2 — Status filter applied:**
- **Given** a request with `?status=Confirmed`
- **When** `GET /api/host/bookings` is called
- **Then** only Bookings with `status=Confirmed` are returned

**AC-3 — Host sees only their own Cabins' Bookings:**
- **Given** Bookings for Cabins owned by different Hosts
- **When** Host A calls `GET /api/host/bookings`
- **Then** only Bookings for Cabins where `host_id = Host A's user ID` are returned

**AC-4 — Unauthenticated request rejected:**
- **Given** no valid JWT
- **When** `GET /api/host/bookings` is called
- **Then** HTTP 401

**AC-5 — Results are paginated:**
- **Given** more Bookings than `pageSize`
- **When** called with `page` and `pageSize` params
- **Then** paginated results returned with `totalCount`

### Frontend — `/host/bookings`

**AC-6 — Booking table displayed:**
- **Given** a Host with Bookings
- **When** `/host/bookings` loads
- **Then** a table shows cabin name, check-in/check-out dates, guest reference (truncated guest ID), total price, and status badge for each Booking

**AC-7 — Status filter UI:**
- **Given** a status filter dropdown
- **When** the Host selects a status
- **Then** the table updates to show only Bookings with that status

**AC-8 — Empty state:**
- **Given** the Host has no Bookings across their Cabins
- **Then** an "No bookings yet" empty state is shown

**AC-9 — Auth guard:**
- **Given** an unauthenticated user navigates to `/host/bookings`
- **Then** they are redirected to login with a `returnUrl`

## Dependencies

- BKF-002 — Bookings must exist to be listed

## DB Schema Prerequisite

The `cabins` table must have a `host_id` column:
```sql
alter table cabins add column if not exists host_id uuid references auth.users (id);
```
RLS on `cabins` should restrict Host mutations to rows where `host_id = auth.uid()`. The search policy (public read of published cabins) remains unchanged.

## Notes

- `guestId` is shown as a truncated UUID reference (e.g. `abc12345…`) — not the Guest's name or email, as those require a join to `auth.users` which is outside Supabase's RLS boundary for this intent. Full guest details are a future improvement.
- This is the only unit that introduces the concept of Host identity via `cabins.host_id`. If that column does not yet exist, this unit's API will be blocked — the prerequisite schema change is part of this unit's definition of done.

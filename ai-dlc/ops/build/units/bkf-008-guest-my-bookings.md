# Unit: Guest My Bookings

**ID:** BKF-008
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Backend + Frontend
**Priority:** Medium
**Date:** 2026-06-14

---

## Purpose

Provides a paginated API endpoint and a frontend page that shows an authenticated Guest their own Bookings across all statuses, with the ability to cancel Confirmed Bookings from the list.

## Acceptance Criteria

### Backend — `GET /api/bookings`

**AC-1 — Returns authenticated Guest's Bookings:**
- **Given** an authenticated Guest with one or more Bookings
- **When** `GET /api/bookings` is called
- **Then** all Bookings for that Guest are returned (all statuses), sorted by `checkIn` descending, with `cabinName`, `checkIn`, `checkOut`, `totalPrice`, `currency`, and `status`

**AC-2 — Unauthenticated request rejected:**
- **Given** no valid JWT
- **When** `GET /api/bookings` is called
- **Then** HTTP 401

**AC-3 — Results are paginated:**
- **Given** more Bookings than `pageSize`
- **When** `GET /api/bookings?page=1&pageSize=10` is called
- **Then** at most 10 results are returned with `totalCount`

**AC-4 — Returns only the Guest's own Bookings (EC-007):**
- **Given** Bookings from multiple Guests exist in the DB
- **When** `GET /api/bookings` is called by Guest A
- **Then** only Guest A's Bookings are returned; RLS enforces this at the DB layer

### Frontend — `/my-bookings`

**AC-5 — Booking list displayed:**
- **Given** an authenticated Guest with Bookings
- **When** `/my-bookings` loads
- **Then** each Booking shows cabin name, check-in/check-out dates, total price, and a status badge

**AC-6 — Cancel action for Confirmed Bookings:**
- **Given** a Confirmed Booking in the list
- **Then** a "Cancel" button is shown; clicking it prompts confirmation then calls `POST /api/bookings/{id}/cancel`; on success the list refreshes and the Booking shows `Cancelled`

**AC-7 — Empty state shown when no Bookings:**
- **Given** the Guest has no Bookings
- **When** `/my-bookings` loads
- **Then** a "No bookings yet" empty state with a "Search cabins" link is shown

**AC-8 — Auth guard:**
- **Given** an unauthenticated user navigates to `/my-bookings`
- **Then** they are redirected to login with a `returnUrl`

## Dependencies

- BKF-002 — Bookings must exist to be listed
- BKF-003 — Cancel action calls Cancel Booking API

## API Contract

**Endpoint:** `GET /api/bookings`
**Auth:** Required (Bearer JWT)

**Query Parameters:**
| Parameter | Type | Required | Description |
|---|---|---|---|
| `page` | integer | No | 1-based, default 1 |
| `pageSize` | integer | No | Default 10, max 50 |

**Response (200):**
```typescript
{
  data: {
    items: {
      bookingId:  string;
      cabinId:    string;
      cabinName:  string;
      checkIn:    string;
      checkOut:   string;
      totalPrice: number;
      currency:   string;
      status:     "Pending" | "Confirmed" | "Cancelled" | "Completed" | "NoShow";
    }[];
    page:       number;
    pageSize:   number;
    totalCount: number;
  };
  error: null;
}
```

# Unit: Cabin Availability Query API

**ID:** CSA-002
**Status:** Ready
**Intent:** [Cabin Search & Availability](../../inception/intents/2026-06-11-cabin-search-availability.md)
**Layer:** Backend — API
**Priority:** High
**Date:** 2026-05-27 (revised 2026-06-12, elaboration session 1)

---

## Purpose

Expose a public API endpoint that accepts a Date Range and optional filters, and returns the list of Cabins that are fully available for the requested stay, each with a nightly rate breakdown and Total Price, sorted by lowest Total Price.

## Acceptance Criteria

**AC-1 — Returns only available Cabins:**
- **Given** a Cabin has a Confirmed or Pending Booking that overlaps the requested Date Range
- **When** `GET /api/cabins/search` is called with that Date Range
- **Then** that Cabin is excluded from the results

**AC-2 — Blackout Dates block results:**
- **Given** a Cabin has a Blackout Date that overlaps the requested Date Range
- **When** `GET /api/cabins/search` is called
- **Then** that Cabin is excluded from results (EC-004)

**AC-3 — Available Cabin appears with price breakdown:**
- **Given** a Cabin is available for the full Date Range
- **When** `GET /api/cabins/search` is called
- **Then** that Cabin appears in results with a per-rate-group breakdown and a Total Price calculated by CSA-001

**AC-4 — Guest count filter:**
- **Given** the request includes a `guests` parameter
- **When** `GET /api/cabins/search` is called
- **Then** only Cabins whose maximum guest capacity is ≥ the requested `guests` value are returned

**AC-5 — Price range filter:**
- **Given** the request includes `minPrice` and/or `maxPrice`
- **When** `GET /api/cabins/search` is called
- **Then** only Cabins whose calculated Total Price falls within the specified range are returned

**AC-6 — Results sorted by lowest Total Price:**
- **Given** multiple Cabins are available
- **When** `GET /api/cabins/search` is called
- **Then** results are returned in ascending order of Total Price

**AC-7 — Check-out on or before check-in rejected:**
- **Given** the request has `checkOut` ≤ `checkIn`
- **When** `GET /api/cabins/search` is called
- **Then** the API returns HTTP 400 with a validation error message (EC-009)

**AC-8 — Zero-night stay rejected:**
- **Given** `checkIn` equals `checkOut` (same-day)
- **When** `GET /api/cabins/search` is called
- **Then** the API returns HTTP 400 with "Minimum stay is 1 night" (EC-010)

**AC-9 — Dates treated as UTC date-only:**
- **Given** date parameters are submitted as ISO 8601 date strings (e.g. `2026-07-01`)
- **When** the availability query runs
- **Then** comparisons are performed as UTC date-only with no time component (EC-003)

**AC-10 — Public endpoint (no auth required):**
- **Given** an unauthenticated request
- **When** `GET /api/cabins/search` is called with valid parameters
- **Then** the API returns results without requiring a JWT

**AC-11 — Only published Cabins returned:**
- **Given** a Cabin exists but is not published (draft or unlisted)
- **When** `GET /api/cabins/search` is called with any parameters
- **Then** that Cabin never appears in the results, including for its own Host

**AC-12 — Results are paginated:**
- **Given** more available Cabins match than the requested `pageSize`
- **When** `GET /api/cabins/search` is called with `page=1&pageSize=20`
- **Then** at most 20 results are returned, with `totalCount` reflecting the full match count, preserving the Total Price sort order across pages

**AC-13 — Page beyond last returns empty:**
- **Given** the match count is 25 and `pageSize` is 20
- **When** `GET /api/cabins/search` is called with `page=3`
- **Then** an empty `items` array is returned with `totalCount: 25` and HTTP 200

**AC-14 — Invalid pagination parameters rejected:**
- **Given** `page` < 1 or `pageSize` < 1 or `pageSize` > 100
- **When** `GET /api/cabins/search` is called
- **Then** the API returns HTTP 400 `INVALID_PARAMETERS`

**AC-15 — Performance at catalogue scale (non-functional):**
- **Given** a catalogue of 10,000 published Cabins with bookings and blackout data
- **When** `GET /api/cabins/search` is called with a valid Date Range
- **Then** the response is returned in under 1 second

## Edge Cases Addressed

- EC-003 — All date comparisons are UTC date-only (AC-9)
- EC-004 — Blackout Dates always checked in availability query (AC-2)
- EC-009 — Check-out before check-in returns 400 (AC-7)
- EC-010 — Zero-night stay returns 400 (AC-8)

## Dependencies

- CSA-001 — The rate breakdown and Total Price in each result are calculated by the Nightly Rate Breakdown Calculator

## API Contract

**Endpoint:** `GET /api/cabins/search`
**Auth:** Public (no JWT required)

**Query Parameters:**
| Parameter | Type | Required | Description |
|---|---|---|---|
| `checkIn` | `string` (ISO date) | Yes | Inclusive check-in date, e.g. `2026-07-01` |
| `checkOut` | `string` (ISO date) | Yes | Exclusive check-out date, e.g. `2026-07-05` |
| `guests` | `integer` | No | Minimum guest capacity required |
| `amenities` | `string[]` | No | Amenity slugs that must all be present |
| `minPrice` | `decimal` | No | Minimum Total Price for the stay |
| `maxPrice` | `decimal` | No | Maximum Total Price for the stay |
| `page` | `integer` | No | 1-based page number; defaults to 1 |
| `pageSize` | `integer` | No | Results per page; defaults to 20, max 100 |

**Response (200):**
```typescript
{
  data: {
    items: CabinSearchResult[];
    page: number;
    pageSize: number;
    totalCount: number;
  };
  error: null;
}

type CabinSearchResult = {
  cabinId: string;
  name: string;
  description: string;
  imageUrl: string;
  maxGuests: number;
  amenities: string[];
  location: { lat: number; lng: number };   // for map view (CSA-006)
  priceBreakdown: RateLineItem[];
  totalPrice: number;
  currency: string;                          // e.g. "USD"
};

type RateLineItem = {
  label: string;          // e.g. "4 nights × $120 (Base Rate)"
  nights: number;
  ratePerNight: number;
  rateType: "BaseRate" | "SeasonalRate";
  subtotal: number;
};
```

**Response (400):**
```typescript
{
  data: null;
  error: { code: "INVALID_DATE_RANGE" | "ZERO_NIGHT_STAY"; message: string; }
}
```

**Error Codes:**
| Code | Condition |
|---|---|
| 400 `INVALID_DATE_RANGE` | checkOut ≤ checkIn |
| 400 `ZERO_NIGHT_STAY` | checkIn === checkOut |
| 400 `INVALID_PARAMETERS` | Non-parseable date strings, negative guests/price values, or invalid page/pageSize |

## Notes

- No authentication required — browsing is public per the intent assumption.
- The `location` field on each result is included to support the optional map view (CSA-006); if cabin location is not yet modelled, this can be null and CSA-006 can filter those out.
- Amenity filtering is an AND operation — all requested amenities must be present on the Cabin.
- Offset pagination (`page`/`pageSize`/`totalCount`) is in the contract from day one (intent decision 2026-06-12) so the contract never needs a breaking change.
- **Deferred Hold extension:** when the Booking intent introduces Holds, the availability query must also exclude Cabins with an active (non-expired, non-cancelled) Hold overlapping the Date Range. Hold expiry is handled by a background cleanup job (also a Booking-intent unit); this query filters on status only.

# Unit: Cabin Detail Page

**ID:** BKF-005
**Status:** Ready
**Intent:** [Booking Flow](../../inception/intents/2026-06-14-booking-flow.md)
**Layer:** Frontend
**Priority:** High
**Date:** 2026-06-14

---

## Purpose

A page that displays full cabin details and allows an authenticated Guest to select dates and place a Hold, entering the checkout flow. Pre-populates dates when navigating from a search result.

## Acceptance Criteria

**AC-1 — Cabin details displayed:**
- **Given** a valid cabin ID in the route (`/cabins/:id`)
- **When** the page loads
- **Then** cabin name, description, image, max guests, amenities, and location are displayed; page title is the cabin name

**AC-2 — Dates pre-populated from search:**
- **Given** the Guest navigated from a search result with `checkIn` and `checkOut` in the URL state
- **When** the page loads
- **Then** the date picker is pre-populated with those dates and total price is shown immediately

**AC-3 — Total price shown for selected dates:**
- **Given** the Guest has selected a valid date range
- **When** dates change
- **Then** total price is fetched from `GET /api/cabins/search` (or computed client-side via the same rate logic) and displayed; loading state is shown during fetch

**AC-4 — "Reserve" disabled without valid dates:**
- **Given** no dates are selected or `checkOut` ≤ `checkIn`
- **Then** the "Reserve" button is disabled with a descriptive tooltip

**AC-5 — "Reserve" triggers Hold and navigates to Checkout:**
- **Given** valid dates are selected and the Guest is authenticated
- **When** "Reserve" is clicked
- **Then** `POST /api/holds` is called; on success (201) the Guest is navigated to `/checkout?holdId=…`

**AC-6 — Unavailability error handled gracefully:**
- **Given** `POST /api/holds` returns 409 `CABIN_UNAVAILABLE`
- **When** the Guest clicks "Reserve"
- **Then** an inline error message is shown ("These dates are no longer available") and the page refreshes availability

**AC-7 — Unauthenticated Guest redirected to login:**
- **Given** the Guest is not authenticated
- **When** "Reserve" is clicked
- **Then** the Guest is redirected to the login page with a `returnUrl` back to the Cabin Detail Page

## Dependencies

- CSA-005 — Navigates to this page from cabin result cards
- BKF-001 — Calls Place Hold API on "Reserve"

## Notes

- Cabin data is fetched from a new `GET /api/cabins/{id}` endpoint (public, no auth required for viewing). This endpoint is a prerequisite of this unit but small enough to be included in its scope rather than as a separate unit.
- Total price display uses the same NightlyRateCalculator logic as search results. For the frontend, call `GET /api/cabins/search?checkIn=…&checkOut=…` filtered to this cabin, or add a dedicated price endpoint if simpler.

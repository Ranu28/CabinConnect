# Unit: Search Form Component

**ID:** CSA-003
**Status:** Ready
**Intent:** [Cabin Search & Availability](../../inception/intents/2026-06-11-cabin-search-availability.md)
**Layer:** Frontend
**Priority:** High
**Date:** 2026-05-27

---

## Purpose

Render a search form that collects a Date Range, optional guest count, optional amenity filters, and an optional price range from the Guest, validates the input client-side, and triggers a search on submission.

## Acceptance Criteria

**AC-1 — Check-out before check-in is blocked:**
- **Given** the Guest selects a check-out date that is on or before the check-in date
- **When** the Guest attempts to submit the form
- **Then** an inline validation error is displayed and the form is not submitted (EC-009)

**AC-2 — Same-day in/out is blocked:**
- **Given** the Guest selects the same date for check-in and check-out
- **When** the Guest attempts to submit the form
- **Then** an inline validation error reading "Minimum stay is 1 night" is displayed (EC-010)

**AC-3 — Valid submission triggers search:**
- **Given** a valid check-in date, a check-out date at least 1 night later, and optional filter values
- **When** the Guest submits the form
- **Then** the parent component's `onSearch` callback is called with `{ checkIn, checkOut, guests?, amenities?, minPrice?, maxPrice? }`

**AC-4 — Dates submitted as UTC date strings:**
- **Given** the Guest selects dates using the date picker (which displays in local timezone)
- **When** the form calls `onSearch`
- **Then** `checkIn` and `checkOut` are ISO 8601 date-only strings (e.g. `"2026-07-01"`) with no time component (EC-003)

**AC-5 — Filters are optional:**
- **Given** the Guest does not fill in guest count, amenities, or price range
- **When** the form is submitted with only dates
- **Then** the search is triggered with only `checkIn` and `checkOut`; all filter fields are omitted from the payload

**AC-6 — Form re-validates on date change:**
- **Given** the Guest changes the check-in date after already selecting a check-out date
- **When** the new check-in date makes the range invalid (e.g. check-in after check-out)
- **Then** the validation error is shown immediately without requiring re-submission

## Edge Cases Addressed

- EC-003 — Dates always emitted as UTC ISO date strings (AC-4)
- EC-009 — Check-out ≤ check-in blocked client-side (AC-1); server remains authoritative gate
- EC-010 — Zero-night stay blocked client-side (AC-2)

## Dependencies

None — the form is a controlled component; it calls `onSearch` but does not call the API itself. It can be built and tested with a mock callback before CSA-002 is complete.

## API Contract

N/A — frontend component.

## Notes

- Component signature: `<SearchForm onSearch={(params: SearchParams) => void} initialValues?: Partial<SearchParams> />`
- The `initialValues` prop allows pre-populating the form (e.g. from URL query params or a "modify search" flow).
- Date picker must prevent selecting past dates for check-in.
- The amenity filter should render as a multi-select of known amenity slugs; the available slugs can be hardcoded initially and later driven by an API.
- Price range filter renders as a dual-handle slider with a text fallback for accessibility.
- Client-side validation is a UX convenience only — the server (CSA-002) is the authoritative gate.

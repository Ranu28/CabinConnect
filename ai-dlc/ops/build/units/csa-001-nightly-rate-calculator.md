# Unit: Nightly Rate Breakdown Calculator

**ID:** CSA-001
**Status:** Ready
**Intent:** [Cabin Search & Availability](../../inception/intents/2026-06-11-cabin-search-availability.md)
**Layer:** Backend — Domain
**Priority:** High
**Date:** 2026-05-27

---

## Purpose

Given a Cabin and a Date Range, calculate the per-night rate for each night in the stay — applying Base Rate or the winning Seasonal Rate — and return both the night-by-night breakdown and the Total Price.

## Acceptance Criteria

**AC-1 — Base Rate only stay:**
- **Given** a Cabin has a Base Rate and no Seasonal Rates for the requested dates
- **When** the calculator is called with a valid Date Range
- **Then** it returns one line item at the Base Rate for each night, and a Total Price equal to (Base Rate × number of nights)

**AC-2 — Seasonal Rate applies for part of the stay:**
- **Given** a Cabin has a Base Rate and a Seasonal Rate that overlaps some but not all nights of the Date Range
- **When** the calculator is called with that Date Range
- **Then** it returns grouped line items — Base Rate nights listed separately from Seasonal Rate nights — and a Total Price equal to the sum of both groups

**AC-3 — Seasonal Rate covers the full stay:**
- **Given** a Cabin has a Seasonal Rate that covers all nights of the Date Range
- **When** the calculator is called
- **Then** it returns a single group at the Seasonal Rate and Total Price equals (Seasonal Rate × number of nights)

**AC-4 — Overlapping Seasonal Rates: most specific wins:**
- **Given** two Seasonal Rates overlap a night, one with a narrower date range than the other
- **When** the calculator resolves the rate for that night
- **Then** the narrower (more specific) date range wins

**AC-5 — Overlapping Seasonal Rates: equal specificity, higher rate wins:**
- **Given** two Seasonal Rates of identical date range length overlap a night
- **When** the calculator resolves the rate for that night
- **Then** the higher rate is applied (EC-005)

**AC-6 — Zero-night range rejected:**
- **Given** check-in equals check-out (zero nights)
- **When** the calculator is called
- **Then** it throws a domain validation exception with a clear message (EC-010)

## Edge Cases Addressed

- EC-005 — Overlapping Seasonal Rates: most specific date range wins; tie goes to higher rate (AC-4, AC-5)
- EC-010 — Zero-night booking: validated before any calculation (AC-6)

## Dependencies

None — this is a pure domain service with no dependency on other units.

## API Contract

N/A — domain service, not exposed directly as an endpoint.

## Notes

- The calculator is a domain service consumed internally by CSA-002 (Availability Query API).
- "Number of nights" = check-out day (exclusive) minus check-in day. Jun 1–5 = 4 nights.
- Line items in the breakdown should carry: `date`, `rate`, `rateType` (`BaseRate` | `SeasonalRate`), `seasonalRateName?`.
- The calculator must not read from the database; the caller is responsible for passing in the resolved rates. This keeps the service pure and unit-testable.

# Intent: Cabin Search & Availability

**Status:** Elaborated
**Date:** 2026-06-11
**Owner:** RP

---

## What
Guests can search published cabins for a given check-in / check-out date range and see only cabins that are actually available — i.e. no overlapping Confirmed or Pending bookings, no overlapping Holds, and no blackout dates intersecting the range. Each result shows the cabin's key details and the priced-out nightly rate for the requested range so the guest can decide whether to proceed. Hosts can observe how their cabins surface in search results for any given date range, so they can validate their listings are discoverable.

## Why
Search & availability is the front door of CabinConnect. Without it, Guests cannot find cabins to book and the rest of the marketplace has no entry point. Showing unavailable cabins erodes trust quickly — a guest who clicks a "free" cabin and discovers it is taken is a lost conversion. For Hosts, search visibility is the only feedback loop that tells them their cabin is published correctly, priced correctly, and not accidentally blacked out. Getting this surface right is a prerequisite for every downstream feature (Holds, Bookings, Payments).

## Success Looks Like
- A Guest can enter a date range and see a list of cabins that are genuinely available for those exact dates — no false positives.
- Search results include the total price for the requested range computed from Base Rate plus any Seasonal Rate overrides, frozen in display the same way it will be at booking time.
- Blackout dates and existing Confirmed / Pending bookings both correctly exclude a cabin from results.
- A Host can preview search results for their own cabin against any date range and see whether it appears.
- Same-day check-in / check-out (zero-night) requests are rejected with a clear validation message before any search runs.
- Search returns results in under 1 second for a catalogue of up to 10,000 published cabins.

## Assumptions
- Cabin catalogue size at launch is in the low thousands, not millions — a single PostgreSQL query with appropriate indexes is sufficient; we do not need a dedicated search engine.
- Date-only semantics (UTC, no time component) are acceptable for the entire search surface; we do not yet support hourly bookings.
- Filters beyond date range (location, capacity, amenities) can be layered on top of the availability query and do not change its shape.
- Holds count toward unavailability for the duration of the 15-minute window, the same as Pending bookings.
- Search is read-only and does not require write transactions or locks.

## Open Questions
_None remaining — all resolved 2026-06-12. See Decisions below._

## Decisions
| Question | Decision (2026-06-12) |
|---|---|
| Expired Hold handling | Background cleanup job flips expired Holds to Cancelled; availability query filters on status only. Accepted trade-off: a cabin may briefly appear unavailable between Hold expiry and the next job run. Job frequency to be set during elaboration. |
| Partial Seasonal Rate pricing | Show a single total in search result cards, with an expandable per-night breakdown on the cabin detail view. Aligns with EC-005 (most specific range wins) and EC-006 (price frozen at confirmation). |
| Draft cabins in Host preview | Published cabins only. Host preview shows exactly what Guests see; drafts are validated in the listing editor, not search. |
| Search auth | Public endpoint — no authentication required to search. Explicitly marked public per project rules; auth begins at the Hold/Booking step. |
| Pagination | Pagination in the API contract from day one (page size + cursor/offset decided during elaboration), so the contract never needs a breaking change. |

## Out of Scope
- The booking flow itself — placing a Hold, taking payment, confirming a Booking. Search ends at "here are available cabins"; clicking through is a separate Intent.
- Reviews, ratings, and review-based ranking.
- Map-based search and geospatial filtering.
- Saved searches, alerts, or notifications when a cabin becomes available.
- Host-facing analytics dashboards beyond the simple "does my cabin appear?" preview.
- Multi-currency display and FX conversion.

---

## Elaboration Sessions
<!-- Filled in as sessions are run. -->

| Session | Date | Units Extracted |
|---|---|---|
| [Session 1](../elaborations/cabin-search-availability/2026-06-12-session-1.md) | 2026-06-12 | 6 (adopted from 2026-05-27 baseline; 2 revised) |

## Extracted Units
<!-- Summary list. Canonical definitions live in build/units/. -->

| Unit | File | Status |
|---|---|---|
| CSA-001 Nightly Rate Breakdown Calculator | [csa-001](../../build/units/csa-001-nightly-rate-calculator.md) | Ready |
| CSA-002 Cabin Availability Query API | [csa-002](../../build/units/csa-002-cabin-availability-query-api.md) | Ready |
| CSA-003 Search Form Component | [csa-003](../../build/units/csa-003-search-form-component.md) | Ready |
| CSA-004 Cabin Result Card | [csa-004](../../build/units/csa-004-cabin-result-card.md) | Ready |
| CSA-005 Search Results List View | [csa-005](../../build/units/csa-005-search-results-list-view.md) | Ready |
| CSA-006 Map View (Optional Toggle) | [csa-006](../../build/units/csa-006-map-view.md) | Ready |

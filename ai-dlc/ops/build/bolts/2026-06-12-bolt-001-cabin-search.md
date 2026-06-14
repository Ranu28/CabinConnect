# Bolt: Cabin Search & Availability — Full Slice

**ID:** BOLT-001
**Status:** Planned
**Dates:** 2026-06-12 → 2026-06-19 (planned)
**Driver:** RP
**Intent(s):** [Cabin Search & Availability](../../inception/intents/2026-06-11-cabin-search-availability.md)

---

## Goal
A Guest can search for cabins by date range (with optional guest, amenity, and price filters) and browse paginated, price-sorted, genuinely available results in list or map view — end to end, frontend to database.

## Units in Scope

| Order | Unit | Layer | Priority | Status at Plan | Notes |
|---|---|---|---|---|---|
| 1 | [CSA-001](../units/csa-001-nightly-rate-calculator.md) | Backend — Domain | High | Ready | Pure domain service; no dependencies. Build first — everything prices through it |
| 2 | [CSA-003](../units/csa-003-search-form-component.md) | Frontend | High | Ready | Parallel with CSA-001; controlled component, mock `onSearch` callback |
| 3 | [CSA-004](../units/csa-004-cabin-result-card.md) | Frontend | High | Ready | Parallel with CSA-001; renders fixture `CabinSearchResult` data |
| 4 | [CSA-002](../units/csa-002-cabin-availability-query-api.md) | Backend — API | High | Ready | Needs CSA-001. The largest unit: 15 ACs incl. pagination and the <1s @ 10k performance AC |
| 5 | [CSA-005](../units/csa-005-search-results-list-view.md) | Frontend | High | Ready | Integration point — needs CSA-002 (live API), CSA-003, CSA-004 |
| 6 | [CSA-006](../units/csa-006-map-view.md) | Frontend | Low | Ready | **Stretch.** Only start if 1–5 are done with time remaining; dropping it does not fail the bolt |

## Build Sequence

Two parallel tracks, converging at CSA-005:

```
Backend track:   CSA-001 ──→ CSA-002 ──┐
                                       ├──→ CSA-005 ──→ (stretch: CSA-006)
Frontend track:  CSA-003, CSA-004 ─────┘
```

- CSA-003 and CSA-004 have no dependencies — they build and test against mocks/fixtures while the backend track runs.
- CSA-002 consumes CSA-001 in-process, so CSA-001's contract (line items, rate-resolution rules) must be stable before CSA-002 starts.
- CSA-005 is deliberately last: it is the only unit that needs the real API, and it integrates all three prior frontend/backend pieces.
- CSA-006 reuses CSA-004 inside pin popovers and consumes CSA-005's already-fetched results; it adds no API work.

## Definition of Done
- [ ] Every unit's acceptance criteria pass with tests (CSA-001: 6 ACs, CSA-002: 15, CSA-003: 6, CSA-004: 5, CSA-005: 10)
- [ ] CSA-002 performance AC verified against a seeded 10,000-cabin dataset
- [ ] Review checklist (`skills/review-checklist.md`) completed per unit
- [ ] Prompt logs written to `prompts/` for each AI-assisted generation
- [ ] Backlog statuses updated as units move
- [ ] RLS policies confirmed on any new/touched Supabase tables (cabins, bookings, blackout_dates, rates)
- [ ] Retro written in `ops/operate/retros/`

## Risks / Watch Items
- **EC-005 rate resolution (CSA-001)** — "most specific wins, tie → higher rate" is the subtlest logic in the bolt; test overlapping-rate permutations exhaustively before CSA-002 builds on it.
- **Performance AC (CSA-002)** — the availability query joins bookings + blackout dates + rates across 10k cabins with pagination; index design (date-range overlap checks) is the likely failure point. Validate early with the seeded dataset, not at bolt close.
- **EC-003 date handling at every boundary** — UTC date-only must hold across the date picker (CSA-003), API parsing (CSA-002), and SQL comparisons; a single timezone leak breaks availability correctness.
- **Big-batch risk (accepted)** — all six units in one bolt means a long stretch without a done checkpoint; the parallel tracks mitigate, and CSA-006 is pre-declared droppable.
- **Map provider choice (CSA-006)** — undecided (Mapbox / Google / Leaflet+OSM); decide before starting the stretch unit, not during.

## Out of Scope
- Hold exclusion in the availability query and the Hold expiry cleanup job (deferred to the Booking intent — see CSA-002 notes)
- Cabin detail page and per-night price breakdown
- Booking flow of any kind
- Search ranking beyond price sort, saved searches, alerts

---

## Outcome
**Completed:** 2026-06-14
**Units done:** 5 of 6 (CSA-001 through CSA-005 done; CSA-006 stretch deferred)
**Retro:** [2026-06-14-bolt-001-retro.md](../operate/retros/2026-06-14-bolt-001-retro.md)

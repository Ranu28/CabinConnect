# Unit: Cabin Result Card

**ID:** CSA-004
**Status:** Ready
**Intent:** [Cabin Search & Availability](../../inception/intents/2026-06-11-cabin-search-availability.md)
**Layer:** Frontend
**Priority:** High
**Date:** 2026-05-27

---

## Purpose

Render a single Cabin search result as a card, displaying the Cabin's name, image, capacity, amenities, and the nightly rate breakdown with Total Price for the requested stay.

## Acceptance Criteria

**AC-1 — Base Rate only display:**
- **Given** a `CabinSearchResult` with a single `RateLineItem` of type `BaseRate`
- **When** the card is rendered
- **Then** it shows the cabin name, image, max guest count, amenity badges, one rate line (e.g. "4 nights × $120 Base Rate = $480"), and the Total Price

**AC-2 — Multi-rate breakdown display:**
- **Given** a `CabinSearchResult` with multiple `RateLineItem` entries (e.g. 2 nights Base Rate + 3 nights Seasonal Rate)
- **When** the card is rendered
- **Then** each rate group is shown as a separate labelled line, and the Total Price equals the sum of all line subtotals

**AC-3 — Total Price is prominent:**
- **Given** any `CabinSearchResult`
- **When** the card is rendered
- **Then** the Total Price is displayed in a visually prominent position (e.g. larger font or highlighted) with the currency symbol

**AC-4 — Missing image handled gracefully:**
- **Given** a `CabinSearchResult` where `imageUrl` is null or empty
- **When** the card is rendered
- **Then** a placeholder image or icon is shown; no broken image element is displayed

**AC-5 — Card is a navigation target:**
- **Given** the card is rendered inside a list
- **When** the Guest clicks the card
- **Then** the `onSelect` callback is called with the `cabinId`, enabling the parent to navigate to the Cabin detail page

## Edge Cases Addressed

None specific to EC-001–EC-010 — this is a pure display component consuming already-validated data from the API.

## Dependencies

None — the card accepts a `CabinSearchResult` prop and renders it. It can be built and tested with fixture data before CSA-002 is complete.

## API Contract

N/A — frontend component.

## Notes

- Component signature: `<CabinResultCard result={CabinSearchResult} onSelect={(cabinId: string) => void} />`
- The `CabinSearchResult` type is defined in CSA-002's API contract.
- Amenity badges should render a maximum of N badges (e.g. 4) with a "+N more" overflow; exact count is a design decision.
- The card should be keyboard-navigable and have an accessible label (e.g. `aria-label="View {cabinName} — {totalPrice} total"`).

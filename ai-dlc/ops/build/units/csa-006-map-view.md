# Unit: Map View (Optional Toggle)

**ID:** CSA-006
**Status:** Ready
**Intent:** [Cabin Search & Availability](../../inception/intents/2026-06-11-cabin-search-availability.md)
**Layer:** Frontend
**Priority:** Low
**Date:** 2026-05-27

---

## Purpose

Provide an optional map view of search results, toggled alongside the list view, showing Cabin pins at their geographic coordinates with their Total Price as a label.

## Acceptance Criteria

**AC-1 — Toggle between list and map:**
- **Given** search results are loaded
- **When** the Guest clicks "Map view"
- **Then** the list of Cabin Result Cards is replaced by a map displaying one pin per available Cabin; the toggle control reflects the active view

**AC-2 — Toggle back to list:**
- **Given** the map view is active
- **When** the Guest clicks "List view"
- **Then** the Cabin Result Card list is shown again in the same state as before the toggle

**AC-3 — Pin shows Total Price:**
- **Given** the map view is rendered
- **When** a Cabin pin is displayed
- **Then** the pin label shows the Total Price for the stay (matching the value on the Cabin Result Card)

**AC-4 — Clicking a pin shows the Cabin card:**
- **Given** the map view is rendered
- **When** the Guest clicks a Cabin pin
- **Then** a popover or drawer appears containing the Cabin Result Card (CSA-004) for that Cabin

**AC-5 — Cabins without location are excluded from map:**
- **Given** a `CabinSearchResult` has a null `location`
- **When** the map view is rendered
- **Then** that Cabin is not pinned on the map (it remains available in the list view)

## Edge Cases Addressed

None specific to EC-001–EC-010.

## Dependencies

- CSA-004 — reuses the Cabin Result Card inside the pin popover
- CSA-005 — the map view toggle lives inside the Search Results List View

## API Contract

N/A — frontend component, consumes the same `CabinSearchResult[]` already fetched by CSA-005.

## Notes

- This unit is **Low priority** — ship CSA-001 through CSA-005 first; add the map view in a later bolt.
- Map provider (e.g. Mapbox, Google Maps, Leaflet + OSM) is a design decision to be made before implementation.
- The map does not make any additional API calls; it renders from the same result set already loaded by CSA-005.
- Cabin `location` coordinates must be added to the Cabin entity and the CSA-002 response if not already present.

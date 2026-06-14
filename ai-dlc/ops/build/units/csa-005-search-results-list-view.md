# Unit: Search Results List View

**ID:** CSA-005
**Status:** Ready
**Intent:** [Cabin Search & Availability](../../inception/intents/2026-06-11-cabin-search-availability.md)
**Layer:** Frontend
**Priority:** High
**Date:** 2026-05-27 (revised 2026-06-12, elaboration session 1)

---

## Purpose

Orchestrate the full cabin search experience: render the Search Form, call the Availability API when search parameters change, and display the resulting Cabin Result Cards in a sorted list, handling loading, empty, and error states.

## Acceptance Criteria

**AC-1 — Results displayed on search:**
- **Given** the Guest submits valid search parameters via the Search Form
- **When** the API returns a successful response
- **Then** a Cabin Result Card (CSA-004) is rendered for each result, in the order returned by the API (lowest Total Price first)

**AC-2 — Loading state shown:**
- **Given** the Guest has submitted a search
- **When** the API request is in flight
- **Then** a loading indicator is displayed and the previous results (if any) are replaced or visually suppressed

**AC-3 — Empty state shown:**
- **Given** the API returns an empty results array
- **When** the list view renders
- **Then** an empty state message is shown (e.g. "No cabins available for these dates — try adjusting your dates or filters")

**AC-4 — Error state shown:**
- **Given** the API returns an error or the network request fails
- **When** the list view renders
- **Then** a user-friendly error message is displayed and no partial results are shown

**AC-5 — Re-search on parameter change:**
- **Given** the Guest modifies and re-submits the Search Form
- **When** new search parameters are provided
- **Then** the API is called again with the new parameters and the results list updates accordingly

**AC-6 — No results shown before first search:**
- **Given** the page has just loaded and no search has been submitted
- **When** the list view renders
- **Then** neither results nor the empty state are shown; only the Search Form is visible

**AC-7 — Pagination controls shown when more pages exist:**
- **Given** the API response has `totalCount` greater than `pageSize`
- **When** the results render
- **Then** pagination controls are displayed showing the current page and total page count

**AC-8 — Navigating to the next page loads new results:**
- **Given** the Guest is viewing page 1 of a multi-page result set
- **When** the Guest navigates to page 2
- **Then** the API is called with `page=2` and the same search parameters, the new results replace the current list, and the view scrolls to the top of the results

**AC-9 — Last-page boundary:**
- **Given** the Guest is viewing the final page of results
- **When** the results render
- **Then** the "next page" control is disabled or hidden

**AC-10 — New search resets to page 1:**
- **Given** the Guest is viewing page 3 of results
- **When** the Guest submits new search parameters via the Search Form
- **Then** the API is called with `page=1` and the pagination state resets

## Edge Cases Addressed

None specific to EC-001–EC-010 — this unit handles UI state orchestration. Data correctness is guaranteed by CSA-002.

## Dependencies

- CSA-002 — calls the Cabin Availability Query API
- CSA-003 — embeds the Search Form Component
- CSA-004 — renders each result as a Cabin Result Card

## API Contract

N/A — frontend component.

## Notes

- This is the top-level page component (or a major section of it) for the search experience.
- The search results list and the optional Map View (CSA-006) are toggled siblings within this component.
- URL query params should reflect the current search state so the page is shareable/bookmarkable (e.g. `?checkIn=2026-07-01&checkOut=2026-07-05&guests=2&page=2`), including the current page.
- Avoid debouncing the search trigger — search fires only on explicit form submission, not on every keystroke.

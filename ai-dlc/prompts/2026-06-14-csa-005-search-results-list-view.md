# Prompt Log — CSA-005 Search Results List View

**Date:** 2026-06-14
**Unit:** [CSA-005](../ops/build/units/csa-005-search-results-list-view.md)
**Bolt:** [BOLT-001](../ops/build/bolts/2026-06-12-bolt-001-cabin-search.md)
**Engineer:** RP
**AI Model:** claude-sonnet-4-6

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CSA-005 — orchestration component integrating SearchForm (CSA-003), CabinResultCard (CSA-004), and /api/cabins/search (CSA-002) |
| **Constraints** | TypeScript strict, no any, no debounce (submit-only), URL query params synced, co-located tests, scroll-to-top on page change |
| **Acceptance Criteria** | 10 ACs — results/loading/empty/error states, re-search, idle state, pagination controls/next-page/last-page/reset |
| **Output Format** | search-results-view.tsx + use-cabin-search.ts hook + co-located test (Vitest + RTL) |

---

## Prompts Issued

### 1 — Start unit
**Prompt:** "start CSA-005"
**Output:** Installed react-router-dom; created use-cabin-search.ts hook, search-results-view.tsx, and search-results-view.test.tsx. Two fixes needed on first run: (1) `scrollIntoView` not available in jsdom — switched to `window.scrollTo` + added stub in test-setup.ts; (2) AC-9 test fixture was wrong (totalCount=1,pageSize=1 gives 1 page, not multiple) — fixed to navigate to last page explicitly. All 24 tests passed after fixes.

---

## Output Summary

### Files created / modified

| File | Purpose |
|---|---|
| `src/features/search/use-cabin-search.ts` | Custom hook: fetch + status state machine (idle/loading/success/error) |
| `src/features/search/search-results-view.tsx` | Orchestration view: SearchForm + results list + pagination + URL sync |
| `src/features/search/search-results-view.test.tsx` | 11 tests covering all 10 ACs (AC-7 has 2 cases: shown and hidden) |
| `src/test-setup.ts` | Added `window.scrollTo = () => {}` stub for jsdom |

### Test results
```
Tests  24 passed (24) — CSA-003 (6) + CSA-004 (7) + CSA-005 (11), 0 failed
```

---

## Review Checklist Result

All items passed. Key notes:
- AC-6 (idle): status starts as 'idle'; results and empty-state only render on 'success'
- AC-2 (loading): `role="status"` shown immediately on submit, cleared when response arrives
- AC-4 (error): `role="alert"` shown; result list is NOT rendered (guarded by `status === 'success'`)
- AC-8 (scroll to top): `window.scrollTo({ top: 0 })` on page change; stubbed in jsdom
- AC-10 (reset page): `handleSearch` always passes `page: 1` to both `setSearchParams` and `search()`
- URL sync: `useSearchParams` from react-router-dom; `buildQueryString` helper exported for reuse

---

## Decisions & Trade-offs

| Decision | Rationale |
|---|---|
| `useRef` for currentParams/currentPage instead of useState | Avoids stale-closure issues in callbacks without triggering extra renders |
| `buildQueryString` exported from use-cabin-search.ts | Reused by the view for both URL sync and the API call; single source of truth for param serialisation |
| `window.scrollTo` over `element.scrollIntoView` | scrollIntoView not implemented in jsdom; scrollTo is a no-op in jsdom, no mock needed at component level |
| react-router-dom v6 useSearchParams | Standard URL sync for React SPAs; required for shareable/bookmarkable search state (unit notes) |

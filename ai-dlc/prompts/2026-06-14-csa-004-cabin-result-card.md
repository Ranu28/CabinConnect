# Prompt Log — CSA-004 Cabin Result Card

**Date:** 2026-06-14
**Unit:** [CSA-004](../ops/build/units/csa-004-cabin-result-card.md)
**Bolt:** [BOLT-001](../ops/build/bolts/2026-06-12-bolt-001-cabin-search.md)
**Engineer:** RP
**AI Model:** claude-sonnet-4-6

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CSA-004 — pure display component, fixture CabinSearchResult data, no API dependency |
| **Constraints** | TypeScript strict, kebab-case files, accessible (aria-label, keyboard nav), max 4 amenity badges + overflow |
| **Acceptance Criteria** | 5 ACs — base rate display, multi-rate display, prominent total, missing image placeholder, onSelect callback |
| **Output Format** | cabin-result-card.tsx + co-located cabin-result-card.test.tsx (Vitest + RTL) |

---

## Prompts Issued

### 1 — Start unit
**Prompt:** "start CSA-004"
**Output:** Read CSA-002 API contract to get exact CabinSearchResult/RateLineItem types; extended search-types.ts; created cabin-result-card.tsx and cabin-result-card.test.tsx. One test failure on first run (aria-label on wrong element) — fixed immediately, all 13 tests passed on second run.

---

## Output Summary

### Files created / modified

| File | Purpose |
|---|---|
| `src/features/search/search-types.ts` | Extended with `RateLineItem` and `CabinSearchResult` from CSA-002 contract |
| `src/features/search/cabin-result-card.tsx` | Display card with image fallback, amenity overflow, keyboard nav, prominent total |
| `src/features/search/cabin-result-card.test.tsx` | 7 tests covering all 5 ACs + overflow edge case |

### Test results
```
Tests  13 passed (13)  — CSA-003 (6) + CSA-004 (7), 0 failed
```

---

## Review Checklist Result

All items passed. Key notes:
- AC-3: `aria-label` on `<strong>` (not parent `<p>`) so `toHaveAccessibleName` resolves correctly
- AC-4: `imageUrl: null` renders `role="img"` div placeholder; no `<img>` element created
- AC-5: `tabIndex={0}` + `onKeyDown` (Enter/Space) makes card keyboard-navigable
- Amenity overflow: `MAX_VISIBLE_AMENITIES = 4` constant; "+N more" shown when exceeded
- `CabinSearchResult` types sourced directly from CSA-002 API contract — no guessing

---

## Decisions & Trade-offs

| Decision | Rationale |
|---|---|
| Types defined in search-types.ts (not a separate api-types file) | All types are search-feature-scoped; a shared api/ layer can be introduced when a second feature needs them |
| `Intl.NumberFormat` for currency | Locale-aware, no extra dependency; currency code comes from the API response |
| Emoji placeholder for missing image | Temporary; replaced with a proper SVG/icon when a UI library is chosen |

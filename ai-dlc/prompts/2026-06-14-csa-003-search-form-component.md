# Prompt Log — CSA-003 Search Form Component

**Date:** 2026-06-14
**Unit:** [CSA-003](../ops/build/units/csa-003-search-form-component.md)
**Bolt:** [BOLT-001](../ops/build/bolts/2026-06-12-bolt-001-cabin-search.md)
**Engineer:** RP
**AI Model:** claude-sonnet-4-6

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CSA-003 — frontend controlled component, no API dependency, built against mock onSearch callback |
| **Constraints** | TypeScript strict, no any, kebab-case files, EC-003/EC-009/EC-010 client-side only, server remains authoritative |
| **Acceptance Criteria** | 6 ACs — date validation, onSearch payload shape, ISO date strings, optional filters omitted |
| **Output Format** | search-form.tsx + search-types.ts + co-located search-form.test.tsx (Vitest + RTL) |

---

## Prompts Issued

### 1 — Start unit
**Prompt:** "start CSA-003"
**Output:** Installed Vitest + React Testing Library; added strict mode and DOM.Iterable to tsconfig; configured vitest in vite.config.ts; created search-types.ts, search-form.tsx, search-form.test.tsx.

---

## Output Summary

### Files created

| File | Purpose |
|---|---|
| `src/features/search/search-types.ts` | SearchParams interface + AMENITY_SLUGS constant |
| `src/features/search/search-form.tsx` | Controlled form component with inline date validation |
| `src/features/search/search-form.test.tsx` | 6 tests, one per AC |
| `src/test-setup.ts` | Imports @testing-library/jest-dom matchers |

### Test results
```
Tests  6 passed (6)
```

---

## Review Checklist Result

All items passed. Key notes:
- EC-003: `<input type="date">` always returns "YYYY-MM-DD" regardless of locale — no conversion needed
- EC-009: `checkOut < checkIn` blocked on submit and on date change
- EC-010: `checkIn === checkOut` shows "Minimum stay is 1 night" message
- AC-6 (re-validate on change): handled via `useEffect` watching both date values
- Optional filters (AC-5): only added to payload when non-empty
- No auth/RLS items applicable (frontend component, no API calls)

---

## Decisions & Trade-offs

| Decision | Rationale |
|---|---|
| Native `<input type="date">` over a date-picker library | No UI library decided yet; native input is accessible and returns ISO date strings natively |
| Checkboxes for amenities, number inputs for price | No UI library for slider yet; accessible text fallback matches the unit's requirement |
| useEffect for AC-6 re-validation | Keeps validation reactive without duplicating logic in each onChange handler |

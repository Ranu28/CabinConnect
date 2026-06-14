# Prompt Log — CSA-001 Nightly Rate Breakdown Calculator

**Date:** 2026-06-14
**Unit:** [CSA-001](../ops/build/units/csa-001-nightly-rate-calculator.md)
**Bolt:** [BOLT-001](../ops/build/bolts/2026-06-12-bolt-001-cabin-search.md)
**Engineer:** RP
**AI Model:** claude-sonnet-4-6

---

## Prompt Quality Gate

| Component | Value |
|---|---|
| **Context** | CSA-001 — pure domain service, no DB or HTTP dependencies; consumed internally by CSA-002 |
| **Constraints** | No DB reads (caller passes rates); C# naming conventions; EC-005 and EC-010 must be handled |
| **Acceptance Criteria** | 6 ACs defined in unit file: base rate only, partial seasonal, full seasonal, narrowest wins, higher rate wins, zero-night rejected |
| **Output Format** | C# source files (Domain project) + xUnit test file covering all ACs |

---

## Prompts Issued

### 1 — Project structure decision
**Prompt:** Recommendation for full-stack monorepo structure (frontend + backend).
**Output:** Proposed `src/backend/` (.NET solution with Domain, Api, Infrastructure projects) + `src/frontend/` (Vite + React 18 + TypeScript strict) + `tests/` split. Confirmed alignment with ADR-004 (monorepo), ADR-002 (.NET API), ADR-003 (React SPA).

### 2 — Scaffold
**Prompt:** "yes, go ahead and scaffold it"
**Output:** Scaffolded full .NET 10 solution (`CabinConnect.slnx`) with 5 projects, project references, FluentAssertions added to test projects, Vite frontend initialised with `npm install` completed.

### 3 — CSA-001 implementation
**Prompt:** "start CSA-001"
**Output:** 6 source files in `CabinConnect.Domain` + 1 test file with 7 tests. All 7 tests passed on first run.

---

## Output Summary

### Files created

| File | Purpose |
|---|---|
| `src/backend/CabinConnect.Domain/Rates/NightlyRateCalculator.cs` | Static service; per-night rate resolution with EC-005 logic |
| `src/backend/CabinConnect.Domain/Rates/RateBreakdown.cs` | Result type: line items + total price |
| `src/backend/CabinConnect.Domain/Rates/RateLineItem.cs` | Per-night record: date, rate, type, name |
| `src/backend/CabinConnect.Domain/Rates/RateType.cs` | Enum: `BaseRate` / `SeasonalRate` |
| `src/backend/CabinConnect.Domain/Rates/SeasonalRate.cs` | Input record: start, end, rate, name |
| `src/backend/CabinConnect.Domain/Exceptions/DomainValidationException.cs` | Domain exception for EC-010 and future validations |
| `tests/CabinConnect.Domain.Tests/Rates/NightlyRateCalculatorTests.cs` | 7 xUnit tests covering all 6 ACs |

### Test results
```
Total tests: 7  |  Passed: 7  |  Failed: 0
```

---

## Review Checklist Result

All items passed. Key notes:
- EC-005 handled: `OrderBy(span).ThenByDescending(rate)` in `ResolveNight`
- EC-010 handled: `checkOut <= checkIn` guard throws `DomainValidationException`
- No auth/RLS items applicable (pure domain service, no endpoints, no new tables)
- No hallucinated APIs — code compiled clean and all tests passed

---

## Decisions & Trade-offs

| Decision | Rationale |
|---|---|
| Static class for calculator | No state, no dependencies — static is the correct choice for a pure function |
| Per-night `RateLineItem` (not grouped) | Caller (CSA-002) can group; keeping items per-night makes the breakdown inspectable and testable at the finest granularity |
| `SeasonalRate.EndDate` is exclusive | Consistent with the domain's date-range convention (inclusive check-in, exclusive check-out) |
| .NET 10 instead of .NET 8 | .NET 10 is the installed SDK; satisfies the "8+" requirement in code-standards.md |

# Improvement: Npgsql Array Parameter Rule

**Date:** 2026-06-14
**Triggered By:** [Retro — BOLT-001](../retros/2026-06-14-bolt-001-retro.md)
**Target File:** [ai-dlc/rules/code-standards.md](../../../rules/code-standards.md)
**Status:** Applied

---

## What to Change

Add a Npgsql-specific Dapper rule under the Backend (.NET) framework patterns section.

### Current

```
- Use repository pattern for data access; no raw SQL in controllers
```

### Proposed

```
- Use repository pattern for data access; no raw SQL in controllers
- **Npgsql + Dapper array parameters:** use `= ANY(@Param)` — never `IN @Param`. Dapper's `IN` expansion is SQL Server–style and produces `IN $1` under Npgsql, which is a PostgreSQL syntax error (42601) at runtime. `= ANY(@Param)` works natively with PostgreSQL array types and is the correct idiom.
```

## Why

BOLT-001 shipped `CabinSearchRepository` with three `IN @CabinIds` and one `IN @ActiveStatuses` clause. All unit and integration tests passed because they mock the repository — the SQL never reaches PostgreSQL. The syntax error only surfaced at `dotnet run`, producing a 500 on the first real search request.

## Expected Outcome

Any future AI-generated or human-written Dapper query targeting PostgreSQL will use `= ANY(@Param)`. The review checklist will catch `IN @` patterns in `.cs` files before they reach `dotnet run`.

## Applied

- [x] Target file updated
- [x] Retro file updated to mark this improvement as applied
- [ ] Team notified (if the change affects how the team uses the process)

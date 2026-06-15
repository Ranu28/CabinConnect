# Improvement: Authentication Must Be in Definition of Done for Any Authenticated Endpoint

**Date:** 2026-06-15
**Triggered by:** [BOLT-002 Retro](../retros/2026-06-15-bolt-002-retro.md)
**Status:** Proposed

---

## Problem

In BOLT-001, `[Authorize]` was placed on all endpoints but the JWT Bearer scheme was never configured. The `FakeAuthHandler` in tests masked this — tests passed using the fake scheme, while any real Supabase token would have returned 401 in production.

This was a carryover stub that lived undetected until it was noticed at the start of BOLT-002. Fixing it required understanding the test override pattern and adding the Supabase OIDC authority configuration to `Program.cs` — work that should have been part of BKF-001 (the first unit to introduce `[Authorize]`).

## Rule

> Any unit that introduces a new `[Authorize]` endpoint must include the following in its Definition of Done:
> 1. The authentication scheme is fully configured (not stubbed) in `Program.cs`
> 2. The `.env.local` requirement for the auth authority URL is documented
> 3. At least one test exercises the 401 path using `FakeAuthHandler` without the auth header
> 4. The prompt log notes which auth claims are read and how they are validated

## Applies To

- Unit template: add "auth configured and tested end-to-end" to the DoD checklist section
- Review checklist: add as a check when any `[Authorize]` attribute is introduced
- BOLT plan: if a bolt introduces the first authenticated endpoint, auth scheme setup is a hard dependency, not an optional step

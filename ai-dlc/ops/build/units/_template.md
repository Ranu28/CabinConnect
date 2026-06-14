# Unit: <Name>

**ID:** <PREFIX-NNN>
**Status:** Draft | Ready | In Progress | Done | Deferred
**Intent:** [Intent Name](../../inception/intents/<intent-slug>.md)
**Layer:** Backend — Domain | Backend — API | Frontend | Full-Stack
**Priority:** High | Med | Low
**Date:** YYYY-MM-DD

---

## Purpose
<!-- One sentence. What behaviour does this unit add to the system? -->

## Acceptance Criteria
<!-- Given/When/Then format. One behaviour per criterion. -->

**AC-1:**
- **Given** 
- **When** 
- **Then** 

**AC-2:**
- **Given** 
- **When** 
- **Then** 

## Edge Cases Addressed
<!-- Which EC-XXX items from guidelines/edge-cases.md apply here? -->
- EC-XXX — <how this unit handles it>

## Dependencies
<!-- Other units that must be Done before this unit can start. -->
- <PREFIX-NNN> — <reason>

## API Contract
<!-- For Backend — API units only. Leave blank for domain/frontend units. -->

**Endpoint:** `METHOD /api/path`
**Auth:** Public | JWT required
**Request:**
```
{
}
```
**Response:**
```
{
}
```
**Error Codes:**
| Code | Condition |
|---|---|
| 400 | |
| 401 | |
| 404 | |

## Notes
<!-- Anything the implementer should know that isn't captured above. -->

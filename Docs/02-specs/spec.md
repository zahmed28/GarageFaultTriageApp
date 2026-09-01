# Spec (v1)

**Version:** 1.1  
**Status:** Approved  
**Brief:** [raw.md](../01-requirements/raw.md)  
**See also:** [Design.md](../03-design/design.md) · [Tasks.md](../04-plans/Tasks.md)

---

## 1. Problem

A garage receives unstructured customer descriptions of vehicle faults. Service advisers must turn that text into something a workshop can act on: which system is involved, how urgent it is, what to check, and what to ask the customer next.

v1 solves **structured triage**. It does not diagnose, book work, or manage jobs.

The application must demonstrate a senior engineering shape (clean architecture, one vertical slice, testable boundaries) while remaining an evening-sized product.

---

## 2. Goals and non-goals

### 2.1 Goals

- Accept free-text fault descriptions from a service adviser.
- Return a structured triage result suitable for a workshop conversation.
- Treat language-model output as untrusted; domain rules own safety and validity.
- Expose a single HTTP use case with RFC 7807 errors.
- Run without an API key (fake analysis engine as default).

### 2.2 Non-goals (v1)

- Authentication, authorization, multi-tenancy.
- Persistence (EF Core, DbContext, repositories, saved assessments).
- Job cards, work orders, technicians, scheduling, parts, inventory, VIN/registration lookup, customer portal.
- Generic repository, AutoMapper as a default, extra feature slices.


### 2.3 Later work (not v1)

Persistence of assessments, auth, job cards, adviser accounts, real vehicle data, retries/circuit breaker beyond a single timeout.

Candidates for “with more time”: save assessments, job-card draft from triage, auth, richer safety matrix, observability dashboards.

---

## 3. Actors and UX

| Actor | Uses the app? | Role |
|-------|----------------|------|
| Service adviser | Yes | Pastes customer wording, reads triage, asks follow-up questions. |
| Customer | No | Source of the free text only. |
| Workshop / technician | Indirect | Consumes checks and urgency via the adviser. |

**Disclaimer (always visible in UI):** output is a triage aid, not a diagnosis or a decision to repair.

**Flow:** enter text → submit → loading → result **or** ProblemDetails error.

---

## 4. Use case: Analyse fault

**Name:** AnalyseFault  
**Trigger:** Adviser submits a fault description.  
**Precondition:** Description length is 10–2000 characters after trim.  
**Success:** HTTP 200 with a structured assessment.  
**Failure:** HTTP 400 / 422 / 503 / 504 / 500 as defined in §6.

### 4.1 Input

| Field | Type | Rules |
|-------|------|--------|
| `description` | string | Required; trim; min 10, max 2000 characters. |

### 4.2 Output (success)

| Field | Type | Notes |
|-------|------|--------|
| `customerConcern` | string | Short restatement of the customer issue. |
| `vehicleSystem` | enum string | See §5.3. |
| `urgency` | enum string | See §5.3. |
| `symptoms` | string[] | Distinct, order preserved from first occurrence. |
| `workshopChecks` | string[] | Distinct, order preserved. |
| `clarifyingQuestions` | string[] | Distinct, order preserved. May be empty. |
| `safetyWarning` | string or omitted | Present only when domain policy fires. Never taken from the model as-is. |

JSON must not include `safetyWarning` when the policy does not apply (`null` is not used).

### 4.3 Locked product rules

1. Unknown `vehicleSystem` or `urgency` from the analysis engine → reject the candidate (do not map to a default).
2. Duplicate symptoms, checks, or questions → keep the first, ignore later duplicates (ordinal ignore-case).
3. Safety warning: if `vehicleSystem` is `Brakes` **and** `urgency` is `Critical`, set a **fixed** domain string:  
   `Safety: treat as potential brake failure — do not drive; inspect before any other work.`  
   Otherwise omit `safetyWarning`.
4. Urgency is taken from the validated candidate. There is no escalate/de-escalate API in v1.
5. Empty symptom or check lists after normalisation → reject the candidate.

---

## 5. Domain

### 5.1 Bounded context

**Fault assessments.** One aggregate per analyse request. Nothing is stored.

### 5.2 Types

| Type | Kind | Responsibility |
|------|------|----------------|
| `FaultAssessment` | Aggregate | Created only via factory from a validated candidate + original description. |
| `FaultDescription` | Value object | Trim, length 10–2000. |
| `CustomerConcern` | Value object | Non-empty, max 500 characters after trim. |
| `Symptom` | Value object | Non-empty, max 200 characters. |
| `WorkshopCheck` | Value object | Non-empty, max 300 characters. |
| `ClarifyingQuestion` | Value object | Non-empty, max 300 characters. |
| `VehicleSystem` | Enum | Closed set. |
| `Urgency` | Enum | Closed set. |
| `FaultAnalysisCandidate` | Application DTO | Untrusted engine output. **Not** a domain entity. **Not** the HTTP contract. |

The aggregate is never deserialized from LLM JSON. Mapping: candidate → domain factory → `AnalyseFaultResult` → HTTP response (explicit maps).

### 5.3 Enumerations

**VehicleSystem:** `Engine`, `Electrical`, `Transmission`, `Suspension`, `Brakes`, `Cooling`, `Steering`, `Body`.  
`UnknownSystem` is **not** a valid value — unknown strings fail mapping.

**Urgency:** `Low`, `Medium`, `High`, `Critical`.

Engine implementations must emit these exact names (case-insensitive parse). Any other value is a candidate rejection.

### 5.4 Domain exceptions

Used for invariant violations after the command has passed FluentValidation (e.g. candidate mapping failure). Mapped to HTTP in §6.

---

## 6. HTTP API contract

**Endpoint:** `POST /api/fault-assessments/analyse`  
**Content-Type:** `application/json`

### Request

```json
{
  "description": "Customer says the pedal goes to the floor and the car will not stop."
}
```

### Response 200

```json
{
  "customerConcern": "Brake pedal travels to the floor; vehicle does not stop.",
  "vehicleSystem": "Brakes",
  "urgency": "Critical",
  "symptoms": ["Pedal to floor", "No braking"],
  "workshopChecks": ["Inspect brake fluid level and leaks", "Check master cylinder"],
  "clarifyingQuestions": ["Did any warning light appear?"],
  "safetyWarning": "Safety: treat as potential brake failure — do not drive; inspect before any other work."
}
```

When `Ai:Provider` is `Fake`, the **200 body** must match the Fake fixture for the matched id (see [Tasks.md](../04-plans/Tasks.md) — Fake engine fixtures), plus domain `safetyWarning` when Brakes + Critical. The JSON example above is illustrative of shape, not the Fake fixture text.

CORS: allow the Vite development origin in Development. Production CORS is out of v1.

### Errors (ProblemDetails)

All errors use RFC 7807. Include `traceId` in `extensions`. Do not leak stack traces or engine payloads.

| Situation | Status | Type (stable URI fragment) | Body |
|-----------|--------|----------------------------|------|
| FluentValidation failure | 400 | `validation` | `ValidationProblemDetails` with `errors` keyed by field |
| Domain / candidate mapping failure | 422 | `fault-analysis-rejected` | `title`, `detail` (safe message) |
| Engine unavailable | 503 | `analysis-unavailable` | `title`, `detail` |
| Engine timeout | 504 | `analysis-timeout` | `title`, `detail` |
| Unhandled | 500 | `internal` | generic `detail`; `traceId` only |

Frontend displays `title`, `detail`, and field `errors` when present.

---

## 7. Frontend (product)

- Single view: textarea, submit, loading state, result panel, error panel, disclaimer.
- Map ProblemDetails to UI; do not invent error copy that contradicts the API.

Stack and client details: [Design.md](../03-design/design.md), [Tasks.md](../04-plans/Tasks.md).

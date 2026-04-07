# Terranes – Milestones

> **To continue development, tell the AI agent:**
>
> ```
> continue next chunk
> ```
>
> The agent will read this file, find the next chunk with status `not-started`,
> implement it, update the status to `done`, update `Next Chunk`, and log details
> in `rules/completion-log.md`.

> **ENFORCEMENT RULE — Completed chunks must be removed from this file.**
>
> When a chunk is marked `done`:
> 1. Log full details (date, goal, architecture, files created/modified, test counts) in `rules/completion-log.md`.
> 2. **Remove the done row from this file.** Milestones.md contains only `not-started` chunks.
> 3. If an entire phase has no remaining rows, replace the table with: `✅ Phase N complete — see completion-log.md`.
> 4. Update the `Next Chunk` section to point to the next `not-started` chunk.
>
> This rule is mandatory for every AI agent session. Never leave done rows in milestones.md.

## Completed Phases

✅ Phase 1 complete — see `rules/completion-log.md` for full history.

1 src project (Contracts). Initial solution scaffold with rules, docs, and test infrastructure.

✅ Phase 2 complete — see `rules/completion-log.md` for full history.

6 src projects (Models3D, Land, SitePlacement, Quoting, Marketplace, Compliance) + Platform.Api. Core platform services with real in-memory implementations, validation, and REST API.

---

## Phase 3 — Partner Integration

**Goal:** Connect external partners (builders, landscapers, furniture suppliers, solicitors, real estate agents) via standardised APIs.

| Chunk | Scope | Status |
|-------|-------|--------|
| 008 | Builder Integration — volume/custom builder APIs, quote request/response | not-started |
| 009 | Landscaper Integration — landscaping provider APIs, design templates | not-started |
| 010 | Furniture & Interior Integration — supplier catalog, room fitting, pricing | not-started |
| 011 | Smart Home Integration — smart home supplier APIs, compatibility checks | not-started |
| 012 | Solicitor Integration — property lawyer matching, contract templates | not-started |
| 013 | Real Estate Agent Integration — land/home listings sync, MLS feeds | not-started |

---

## Phase 4 — Immersive 3D Experience

**Goal:** Build the immersive 3D virtual village, walkthroughs, and real-time modification tools.

| Chunk | Scope | Status |
|-------|-------|--------|
| 014 | Virtual Village — 3D neighbourhood of fully designed homes | not-started |
| 015 | Home Walkthrough — immersive 3D tour (like Envis/Matterport) | not-started |
| 016 | Real-Time 3D Editor — modify home design on-block in real time | not-started |
| 017 | AI Video-to-3D — record house turning into 3D model | not-started |
| 018 | User-Generated Content — agents/users post their own built homes | not-started |

---

## Phase 5 — Platform Infrastructure

**Goal:** Observability, security, multi-tenancy, deployment.

| Chunk | Scope | Status |
|-------|-------|--------|
| 019 | Authentication & Authorization — OAuth, RBAC, user/agent/partner roles | not-started |
| 020 | Observability — structured logging, health checks, metrics, tracing | not-started |
| 021 | Multi-Tenancy — tenant isolation for partner data, user data, quotes | not-started |
| 022 | Deployment — Kubernetes manifests, Helm charts, CI/CD pipeline | not-started |

---

## Next Chunk

**Chunk 008** — Builder Integration (Phase 3)

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.

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

> **UAT READINESS RULE — Confirm when the app is ready for customer use.**
>
> When the application has a working UI and all backend services are wired end-to-end:
> 1. Confirm explicitly: **"The implementation is ready for UAT."**
> 2. The app must fully work as expected per the requirements — not partially, not with stubs.
> 3. The app must be ready for the customer to use: all features functional, all endpoints wired, all validations in place, all tests passing.
> 4. Do not declare UAT readiness until the Platform.Api (or future web UI) is runnable and exercises every service end-to-end.
> 5. Include clear run instructions (e.g. `dotnet run --project src/Platform.Api`) and a summary of all available endpoints/features.

## Completed Phases

✅ Phase 1 complete — see `rules/completion-log.md` for full history.

1 src project (Contracts). Initial solution scaffold with rules, docs, and test infrastructure.

✅ Phase 2 complete — see `rules/completion-log.md` for full history.

6 src projects (Models3D, Land, SitePlacement, Quoting, Marketplace, Compliance) + Platform.Api. Core platform services with real in-memory implementations, validation, and REST API.

✅ Phase 3 complete — see `rules/completion-log.md` for full history.

PartnerIntegration project with 6 services: Builder, Landscaper, Furniture, SmartHome, Solicitor, RealEstateAgent. All wired into Platform.Api with 30+ partner endpoints.

✅ Phase 4 complete — see `rules/completion-log.md` for full history.

Immersive3D project with 5 services: VirtualVillage, Walkthrough, DesignEditor, VideoToModel, Content. 3D neighbourhood scenes, immersive tours, real-time editing, AI video-to-3D, user-generated content.

✅ Phase 5 complete — see `rules/completion-log.md` for full history.

Infrastructure project with 3 services: Auth, Observability, Tenant. Authentication with hashed passwords and RBAC, structured audit logging with health checks and metrics, multi-tenant isolation.

✅ Phase 6 complete — see `rules/completion-log.md` for full history.

Journey project with 3 services: BuyerJourney, QuoteAggregator, Referral. Full buyer lifecycle orchestration from browsing through to partner referral, cross-partner cost aggregation, qualified lead generation.

✅ Phase 7 complete — see `rules/completion-log.md` for full history.

Notifications project with 3 services: Notification, EventBus, Webhook. In-app notification delivery with read tracking, in-memory pub/sub event bus with correlation, webhook registration and simulated delivery for partners.

✅ Phase 8 complete — see `rules/completion-log.md` for full history.

Analytics project with 3 services: Search, Analytics, Reporting. Cross-entity full-text search with relevance scoring, user engagement tracking with summaries and popular entities, markdown report generation.

✅ Phase 9 complete — see `rules/completion-log.md` for full history.

IntegrationTests project with 56 WebApplicationFactory-based tests exercising every API endpoint group end-to-end. Fixed multi-body parameter binding bugs in 5 partner endpoints (Builder, Landscaper, Solicitor, RealEstateAgent, Webhook).

✅ Phase 10 complete — see `rules/completion-log.md` for full history.

Blazor Server Web UI with 7 pages: Home (landing), Villages (browse/search/detail), Home Designs (gallery/search/detail), Land Blocks (search/test-fit), Marketplace (browse/search/filter), Buyer Journey (guided E2E flow), Dashboard (stats/journeys/notifications). All 29 services wired directly via DI.

✅ Phase 11 complete — see `rules/completion-log.md` for full history.

Vue 3 + Vite + TypeScript frontend replaces Blazor Server. .NET Aspire AppHost orchestrates Platform.Api + Vue frontend together. ServiceDefaults adds OpenTelemetry, health checks, and service discovery.

✅ Phase 12 complete — see `rules/completion-log.md` for full history.

Vue frontend cleanup, reusable components, and 49 Vitest component tests. Old Blazor Web project removed. 4 shared components: LoadingSpinner, StatusBadge, DetailModal, ErrorAlert. All views refactored to use shared components.

---

## Phase 13 — UX & UI Polish (AI-Driven)

> **AI Agent Rule:** Before implementing any chunk in this phase, read `rules/ux-rules.md` for
> component conventions, design principles, and implementation patterns.

**Goal:** Transform the functional Vue 3 frontend into a polished, production-quality UI with
smooth interactions, responsive layouts, accessible controls, and user-friendly feedback.
Each chunk is independently deployable and testable.

| Chunk | Scope | Status |
|-------|-------|--------|
| 050 | **Toast Notifications & Action Feedback** — Create `ToastContainer.vue` + `useToast()` composable. Add success/error toasts to Journey actions (advance stage, request quote, complete). Add loading-disabled state to all async buttons. 6+ Vitest tests. | `done` |
| 051 | **Skeleton Loaders & Smooth Transitions** — Create `SkeletonCard.vue` and `SkeletonTable.vue` components. Replace `<LoadingSpinner>` with skeleton placeholders in all 5 data views. Add Vue `<Transition>` fade on route changes and list enter/leave in card grids. 8+ Vitest tests. | `done` |
| 052 | **Responsive Layout Overhaul** — Migrate sidebar breakpoint from 641px to Bootstrap md (768px). Add collapsible sidebar with slide animation. Make all card grids stack to 1-column on mobile. Add responsive table scrolling. Fix top-bar on mobile. Verify on 320px/768px/1200px. 4+ Vitest tests. | `done` |
| 053 | **Accessibility & Keyboard Navigation** — Add `aria-label` to all buttons and interactive elements. Add Escape-to-close on all modals. Add focus trap inside modals. Add skip-to-content link. Add `aria-live` region for toast announcements. Audit with axe-core rules. 6+ Vitest tests. | `done` |
| 054 | **Dark Mode Support** — Add `useTheme()` composable with system-preference detection + manual toggle. Add theme toggle button in sidebar. Migrate `style.css` hardcoded colours to Bootstrap CSS variables. Sidebar gradient adapts to dark mode. Persist preference in localStorage. 4+ Vitest tests. | `done` |
| 055 | **Enhanced Home Landing Page** — Add hero section with animated gradient background. Add "How it works" 4-step visual flow. Add testimonial carousel (static data). Add footer with links. Add smooth scroll to sections. Mobile-optimised layout. 4+ Vitest tests. | `done` |
| 056 | **Search & Filter UX Improvements** — Add debounced search inputs (300ms) across Villages, Home Models, Land, Marketplace. Add filter chips showing active filters with ×-remove. Add result count badge. Add empty-state illustrations (SVG). Add URL query-string sync for shareable filter URLs. 6+ Vitest tests. | `done` |
| 057 | **Card & List Interaction Polish** — Add hover lift effect on all cards (transform + shadow). Add click-ripple feedback. Add image placeholder gradients on model/village cards. Add pagination component for lists > 12 items. Add sort-by dropdown on Marketplace and Land views. 6+ Vitest tests. | `done` |
| 058 | **Journey UX Enhancement** — Add animated step indicator (horizontal stepper with connecting lines). Add confirmation dialogs before irreversible actions (complete journey). Add journey timeline sidebar showing all past actions with timestamps. Add confetti animation on journey completion. 5+ Vitest tests. | `done` |
| 059 | **Dashboard Widgets & Charts** — Add `StatCard.vue` with animated count-up numbers. Add mini sparkline chart for analytics trends (pure SVG, no chart library). Add notification bell icon in top-bar with unread count badge. Add quick-action buttons (New Journey, Browse Designs). 6+ Vitest tests. | `done` |
| 060 | **Breadcrumbs, Page Titles & Navigation** — Add `BreadcrumbBar.vue` component with auto-generated breadcrumbs from route meta. Add `<title>` updates per route. Add active-page icon highlighting in sidebar. Add "Back to" links in detail modals. Add 404 page. 5+ Vitest tests. | `not-started` |
| 061 | **Form Validation & Input UX** — Add real-time validation on all filter inputs (number ranges, required fields). Add input masking for price fields (AUD format). Add clear-all-filters button. Add auto-focus on first input when views mount. Standardise form-group spacing. 5+ Vitest tests. | `not-started` |
| 062 | **Performance & Bundle Optimisation** — Add route-based code splitting verification. Add image lazy loading for card thumbnails. Add virtual scrolling for large lists (> 50 items). Add web font preloading. Measure and log Lighthouse scores. 3+ Vitest tests. | `not-started` |
| 063 | **Playwright Multi-Browser E2E Tests** — Add Playwright with Chromium, Firefox, WebKit + mobile + tablet viewports. 29 E2E tests across 5 spec files: navigation, home page, responsive layout, views smoke, UX feedback/accessibility. AI agent rule in `rules/playwright-rules.md`. | `done` |

---

## Next Chunk

**Chunk 060** — Breadcrumbs, Page Titles & Navigation.

Read `rules/ux-rules.md` before implementing.

---

For detailed completion history, files created, and notes see `rules/completion-log.md`.

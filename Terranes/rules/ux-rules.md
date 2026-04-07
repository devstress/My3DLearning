# UX & UI Rules — AI Agent Guide

> **Purpose:** This file tells Copilot AI agents how to implement UX/UI chunks for the Terranes Vue 3 frontend.
> Every chunk in Phase 13+ that touches `src/Web.Vue/` must follow these rules.

---

## Technology Stack

| Layer | Tool | Notes |
|-------|------|-------|
| Framework | Vue 3 + TypeScript | Composition API with `<script setup lang="ts">` only |
| Build | Vite 8 | Config in `vite.config.ts` |
| CSS | Bootstrap 5.3 (CDN) + custom `style.css` | No Tailwind, no CSS-in-JS. Use Bootstrap utility classes first, custom CSS only when needed |
| Icons | Bootstrap Icons (SVG inline) | Currently using inline SVG data URIs in `style.css` |
| Testing | Vitest + @vue/test-utils | Tests in `src/__tests__/`. Every new component and composable must have tests |
| Router | vue-router 4 | Lazy-loaded routes in `src/router/index.ts` |
| API | Fetch-based client | `src/api/client.ts`. All API calls return typed promises |
| Aspire | .NET Aspire AppHost | Orchestrates Platform.Api + Vue frontend. Vite proxy for `/api` |

---

## Design Principles

1. **Mobile-first responsive** — All layouts must work on 320px–1920px. Use Bootstrap's grid (`col-sm-*`, `col-md-*`, `col-lg-*`) and responsive utilities.
2. **Accessible** — All interactive elements must have `aria-label` or visible text. Focus indicators must be visible. Keyboard navigation must work (Tab, Enter, Escape for modals).
3. **Feedback-rich** — Every user action must produce visible feedback within 200ms: loading spinners, toast notifications, progress indicators, button disabled states during async ops.
4. **Consistent patterns** — Use the shared components in `src/components/`. If a pattern appears in 2+ views, extract it to a shared component.
5. **No layout shift** — Use skeleton loaders or fixed-height placeholders instead of content that jumps when data loads.
6. **Progressive disclosure** — Show summary first, details on demand. Cards → modals. Tables → expandable rows.
7. **Dark-mode ready** — Use Bootstrap CSS variables (`var(--bs-body-bg)`, `var(--bs-body-color)`) instead of hardcoded colours. Sidebar gradient is the one exception (brand colour).

---

## Component Conventions

### New Component Checklist

When creating a new Vue component:

1. **File location:** `src/components/ComponentName.vue` for shared, `src/views/ViewName.vue` for pages
2. **TypeScript:** All props typed with `defineProps<{}>()`. All emits typed with `defineEmits<{}>()`
3. **Defaults:** Use `withDefaults()` for optional props
4. **Composables:** Extract reusable logic into `src/composables/useXxx.ts` (return refs + functions)
5. **Tests:** Create `src/__tests__/components/ComponentName.spec.ts` with Vitest
6. **No inline styles:** Use Bootstrap utility classes or CSS in `<style scoped>`
7. **Slot-based composition:** Prefer slots over prop drilling for complex content

### Existing Shared Components

| Component | Purpose | Usage |
|-----------|---------|-------|
| `LoadingSpinner` | Shows italic loading message | `<LoadingSpinner message="Loading..." />` |
| `StatusBadge` | Colour-coded badge with status map | `<StatusBadge :status="item.status" />` |
| `DetailModal` | Bootstrap modal wrapper with slot | `<DetailModal :show="bool" title="..." @close="fn"><slot /></DetailModal>` |
| `ErrorAlert` | Red alert for error messages | `<ErrorAlert :message="errorString" />` |

---

## Composable Conventions

When extracting shared logic:

```
src/composables/useXxx.ts
```

Pattern:
```ts
import { ref } from 'vue';

export function useXxx() {
  const state = ref(initialValue);
  function doSomething() { /* ... */ }
  return { state, doSomething };
}
```

Test:
```
src/__tests__/composables/useXxx.spec.ts
```

---

## Toast / Notification Pattern

When a chunk adds toast notifications:

1. Use a `ToastContainer.vue` component mounted once in `App.vue`
2. Use a `useToast()` composable that exposes `showSuccess(msg)`, `showError(msg)`, `showInfo(msg)`
3. Toasts auto-dismiss after 5 seconds. Errors require manual dismiss.
4. Position: bottom-right, stacked.
5. Use Bootstrap's toast classes.

---

## Animation & Transition Rules

1. Use Vue's `<Transition>` and `<TransitionGroup>` for enter/leave animations
2. Keep durations ≤ 300ms (CSS `transition: 0.3s ease`)
3. Use `transform` and `opacity` only — no `height`/`width` animations (cause reflow)
4. Respect `prefers-reduced-motion`: wrap all animations with `@media (prefers-reduced-motion: no-preference)`

---

## Responsive Breakpoints

Follow Bootstrap 5 breakpoints:

| Breakpoint | Class prefix | Min width |
|------------|-------------|-----------|
| Extra small | (none) | 0 |
| Small | `sm` | 576px |
| Medium | `md` | 768px |
| Large | `lg` | 992px |
| Extra large | `xl` | 1200px |

The sidebar currently collapses at 641px (custom). New chunks should migrate this to Bootstrap's `md` breakpoint (768px) for consistency.

---

## Chunk Implementation Pattern

When implementing a UX chunk:

1. **Read this file** and `rules/milestones.md` first
2. **Check existing components** in `src/components/` — reuse before creating
3. **Install npm packages** only if absolutely necessary. Check `gh-advisory-database` for vulnerabilities first
4. **Write the component/composable** following conventions above
5. **Write Vitest tests** for new components and composables
6. **Update existing view tests** if you changed view behaviour
7. **Run `npm test`** in `src/Web.Vue/` to verify all Vitest tests pass
8. **Run `npm run build`** to verify TypeScript compiles and Vite builds
9. **Run `dotnet build && dotnet test`** in `Terranes/` to verify .NET is unbroken
10. **Update milestones.md and completion-log.md** per the standard rules

---

## Forbidden Patterns

- ❌ No jQuery or direct DOM manipulation
- ❌ No CSS-in-JS (styled-components, emotion, etc.)
- ❌ No Tailwind CSS (we use Bootstrap)
- ❌ No global state libraries (Pinia/Vuex) unless a chunk specifically adds one
- ❌ No `any` type — use proper TypeScript types
- ❌ No inline `style=""` attributes — use Bootstrap classes or scoped CSS
- ❌ No hardcoded pixel values for layout — use Bootstrap grid and spacing utilities
- ❌ No suppressing TypeScript errors with `@ts-ignore`

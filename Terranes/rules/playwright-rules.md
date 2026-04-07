# Playwright Rules — AI Agent Guide for E2E Cross-Browser Testing

> **Purpose:** This file tells Copilot AI agents how to write, run, and maintain
> Playwright E2E tests for the Terranes Vue 3 frontend.
> Every UX/UI chunk that adds user-facing behaviour must also add or update E2E tests.

---

## Technology Stack

| Layer | Tool | Notes |
|-------|------|-------|
| E2E Framework | Playwright 1.59+ | Config in `playwright.config.ts` |
| Browsers | Chromium, Firefox, WebKit | All 3 desktop + 2 mobile + 1 tablet = 6 projects |
| Test runner | `@playwright/test` | Tests in `e2e/` directory |
| Dev server | Vite (auto-started) | `webServer` config starts `npm run dev` on port 5173 |

---

## Multi-Browser Projects

The `playwright.config.ts` defines 6 projects for cross-browser and responsive coverage:

| Project | Device | Viewport | Purpose |
|---------|--------|----------|---------|
| `chromium` | Desktop Chrome | 1280×720 | Primary desktop browser |
| `firefox` | Desktop Firefox | 1280×720 | Cross-engine compatibility |
| `webkit` | Desktop Safari | 1280×720 | macOS/iOS rendering engine |
| `mobile-chrome` | Pixel 5 | 393×851 | Android mobile UX |
| `mobile-safari` | iPhone 13 | 390×844 | iOS mobile UX |
| `tablet` | iPad (gen 7) | 810×1080 | Tablet responsive layout |

---

## Running Tests

```bash
cd Terranes/src/Web.Vue

# All browsers (6 projects × all tests)
npm run test:e2e

# Single browser
npm run test:e2e:chromium
npm run test:e2e:firefox
npm run test:e2e:webkit

# Mobile only
npm run test:e2e:mobile

# View HTML report
npm run test:e2e:report
```

---

## Test File Conventions

| Directory | Purpose |
|-----------|---------|
| `e2e/navigation.spec.ts` | Sidebar links, routing, active state highlighting |
| `e2e/home.spec.ts` | Home page hero, cards, CTA buttons |
| `e2e/responsive.spec.ts` | Viewport-specific: sidebar collapse, card stacking, toggler |
| `e2e/views.spec.ts` | Per-view smoke tests: title, search, filters load correctly |
| `e2e/ux-feedback.spec.ts` | Transitions, toasts, skeletons, accessibility basics |

### Naming Convention

```
e2e/{feature-area}.spec.ts
```

Use `test.describe()` blocks to group related scenarios.

---

## Writing a New E2E Test

When implementing a UX chunk that adds user-facing behaviour:

1. **Identify the user flow** — What does the customer do? What do they see?
2. **Write the test first** — Use Playwright's `test()` and `expect()` from `@playwright/test`
3. **Test across viewports** — If the feature behaves differently on mobile vs desktop, add tests in `responsive.spec.ts`
4. **Use semantic selectors** — Prefer `text=`, `role=`, `aria-label=` over CSS selectors
5. **Avoid flaky patterns** — Use `waitForSelector()` or `expect().toBeVisible()` instead of fixed delays

### Selector Priority (best → worst)

1. `page.getByRole('button', { name: 'Submit' })` — accessible role
2. `page.getByText('Welcome')` — visible text
3. `page.locator('[aria-label="Close"]')` — ARIA attribute
4. `page.locator('.nav-link:has-text("Villages")')` — CSS + text
5. `page.locator('#some-id')` — ID (avoid if possible)
6. `page.locator('.some-class')` — CSS class (last resort)

### Test Template

```ts
import { test, expect } from '@playwright/test';

test.describe('Feature Name', () => {
  test('customer-facing scenario description', async ({ page }) => {
    await page.goto('/target-route');
    await expect(page.locator('h2')).toContainText('Expected Title');
    // Test user interaction
    await page.click('button:has-text("Action")');
    // Verify feedback
    await expect(page.locator('.toast')).toBeVisible();
  });
});
```

---

## Customer-First Testing Philosophy

Every test answers the question: **"As a buyer, can I...?"**

- Can I see the home page and understand what this platform does?
- Can I navigate to any section from the sidebar?
- Can I use the app on my phone?
- Can I search and filter properties?
- Can I start a buyer journey and see my progress?
- Can I see feedback when I take an action?

Tests that don't serve a real customer scenario should not be written.

---

## When to Add E2E Tests

| Chunk Type | Required E2E Tests |
|------------|-------------------|
| New page/view | Smoke test (title loads, no errors) + navigation test |
| New component (user-interactive) | Interaction test in relevant feature spec |
| Responsive layout change | Viewport-specific tests in `responsive.spec.ts` |
| Toast/notification change | Feedback verification in `ux-feedback.spec.ts` |
| Accessibility improvement | Semantic structure test in `ux-feedback.spec.ts` |

---

## Forbidden Patterns

- ❌ No `page.waitForTimeout()` — use `waitForSelector()` or `expect().toBeVisible()`
- ❌ No tests that depend on specific API data — the dev server uses in-memory data
- ❌ No snapshot testing for layout — use viewport assertions instead
- ❌ No tests that modify global state without cleanup
- ❌ No hardcoded viewport sizes in tests — use `page.setViewportSize()` only in `responsive.spec.ts`

---

## Integration with UX Rules

This file complements `rules/ux-rules.md`:
- **Vitest** tests verify component logic in isolation (unit/component level)
- **Playwright** tests verify the complete user experience across browsers (E2E level)

Both must pass before a UX chunk is marked `done`.

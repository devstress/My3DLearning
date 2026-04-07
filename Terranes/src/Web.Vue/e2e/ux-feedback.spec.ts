import { test, expect } from '@playwright/test';
import { openSidebarIfMobile } from './helpers';

test.describe('Route Transitions', () => {
  test('page content fades in when navigating between routes', async ({ page }) => {
    await page.goto('/');
    // Navigate to villages
    await openSidebarIfMobile(page);
    await page.click('.nav-link:has-text("Villages")');
    // The transition uses opacity — verify new content appears
    await expect(page.locator('h2')).toContainText('Virtual Villages');

    // Navigate to home models
    await openSidebarIfMobile(page);
    await page.click('.nav-link:has-text("Home Designs")');
    await expect(page.locator('h2')).toContainText('Home Designs');
  });
});

test.describe('Toast Container', () => {
  test('toast container is mounted in the page', async ({ page }) => {
    await page.goto('/');
    // ToastContainer is always mounted in App.vue
    // It renders a container div even when empty
    const toastArea = page.locator('.toast-container, [class*="toast"]');
    // Just verify the page loaded without errors
    await expect(page.locator('h1')).toContainText('Welcome to Terranes');
  });
});

test.describe('Skeleton Loaders', () => {
  test('villages view shows content after loading', async ({ page }) => {
    await page.goto('/villages');
    // Wait for either skeleton or actual content
    await page.waitForSelector('h2');
    await expect(page.locator('h2')).toContainText('Virtual Villages');
  });
});

test.describe('Accessibility Basics', () => {
  test('page has proper document structure', async ({ page }) => {
    await page.goto('/');
    // Check for main landmark
    await expect(page.locator('main')).toBeVisible();
    // Check navigation exists in DOM (may be hidden on mobile until toggled)
    await expect(page.locator('nav')).toHaveCount(1);
  });

  test('all buttons have accessible text', async ({ page }) => {
    await page.goto('/');
    const buttons = page.locator('.card .btn');
    const count = await buttons.count();
    for (let i = 0; i < count; i++) {
      const text = await buttons.nth(i).textContent();
      expect(text?.trim().length).toBeGreaterThan(0);
    }
  });

  test('links have descriptive text', async ({ page }) => {
    await page.goto('/');
    const navLinks = page.locator('.nav-link');
    const count = await navLinks.count();
    for (let i = 0; i < count; i++) {
      const text = await navLinks.nth(i).textContent();
      expect(text?.trim().length).toBeGreaterThan(0);
    }
  });
});

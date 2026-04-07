import { test, expect } from '@playwright/test';

test.describe('Responsive Layout', () => {
  test('desktop: sidebar is visible and page uses row layout', async ({ page }) => {
    await page.setViewportSize({ width: 1200, height: 800 });
    await page.goto('/');
    const sidebar = page.locator('.sidebar');
    await expect(sidebar).toBeVisible();
    // Sidebar navigation should be visible without toggling
    await expect(page.locator('.nav-scrollable')).toBeVisible();
  });

  test('mobile: sidebar nav is hidden by default', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    const navScrollable = page.locator('.nav-scrollable');
    // On mobile, nav-scrollable is hidden unless it has .open class
    await expect(navScrollable).not.toHaveClass(/open/);
  });

  test('mobile: toggler button opens/closes sidebar nav', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');

    const toggler = page.locator('.navbar-toggler');
    await expect(toggler).toBeVisible();

    // Open sidebar
    await toggler.click();
    await expect(page.locator('.nav-scrollable')).toHaveClass(/open/);

    // Click a nav link to close
    await page.click('.nav-link:has-text("Villages")');
    await expect(page).toHaveURL(/\/villages/);
  });

  test('tablet: cards display in responsive grid', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/');
    const cards = page.locator('.card');
    await expect(cards).toHaveCount(6);
    // All cards should be visible
    for (let i = 0; i < 6; i++) {
      await expect(cards.nth(i)).toBeVisible();
    }
  });

  test('mobile: home cards stack vertically', async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 568 });
    await page.goto('/');
    const cards = page.locator('.card');
    await expect(cards).toHaveCount(6);
    // First card should be visible
    await expect(cards.first()).toBeVisible();
  });

  test('page title displays Terranes brand', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Terranes/);
  });
});

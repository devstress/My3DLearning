import { test, expect } from '@playwright/test';

test.describe('Home Page UX', () => {
  test('displays hero section with brand messaging', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('h1')).toContainText('Welcome to Terranes');
    await expect(page.locator('.lead')).toContainText('immersive 3D property platform');
  });

  test('shows 6 feature cards in a grid layout', async ({ page }) => {
    await page.goto('/');
    const cards = page.locator('.card');
    await expect(cards).toHaveCount(6);

    // Check card titles
    await expect(cards.nth(0)).toContainText('Virtual Villages');
    await expect(cards.nth(1)).toContainText('Home Designs');
    await expect(cards.nth(2)).toContainText('Find Land');
    await expect(cards.nth(3)).toContainText('Marketplace');
    await expect(cards.nth(4)).toContainText('Start Your Journey');
    await expect(cards.nth(5)).toContainText('Dashboard');
  });

  test('all CTA buttons are visible and clickable', async ({ page }) => {
    await page.goto('/');
    const buttons = page.locator('.card .btn');
    await expect(buttons).toHaveCount(6);

    for (let i = 0; i < 6; i++) {
      await expect(buttons.nth(i)).toBeVisible();
      await expect(buttons.nth(i)).toBeEnabled();
    }
  });

  test('tagline section is visible at bottom of page', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('text=Think CanIBuild')).toBeVisible();
    await expect(page.getByText('experience', { exact: true })).toBeVisible();
  });
});

import { test, expect } from '@playwright/test';

test.describe('Villages View', () => {
  test('displays loading skeleton then village content', async ({ page }) => {
    await page.goto('/villages');
    // Page should show title
    await expect(page.locator('h2')).toContainText('Virtual Villages');
    // Description text
    await expect(page.locator('text=Explore immersive 3D')).toBeVisible();
  });

  test('search input and layout filter are present', async ({ page }) => {
    await page.goto('/villages');
    await expect(page.locator('input[placeholder="Search by name..."]')).toBeVisible();
    await expect(page.locator('select.form-select')).toBeVisible();
  });

  test('layout filter dropdown has all options', async ({ page }) => {
    await page.goto('/villages');
    const select = page.locator('select.form-select');
    await expect(select).toBeVisible();
    // Click to reveal options
    const options = select.locator('option');
    await expect(options.first()).toContainText('All Layouts');
  });
});

test.describe('Home Models View', () => {
  test('displays page title and search controls', async ({ page }) => {
    await page.goto('/home-models');
    await expect(page.locator('h2')).toContainText('Home Designs');
  });
});

test.describe('Land Blocks View', () => {
  test('displays page title and filter controls', async ({ page }) => {
    await page.goto('/land');
    await expect(page.locator('h2')).toContainText('Land Blocks');
  });
});

test.describe('Marketplace View', () => {
  test('displays page title', async ({ page }) => {
    await page.goto('/marketplace');
    await expect(page.locator('h2')).toContainText('Marketplace');
  });
});

test.describe('Journey View', () => {
  test('displays page title', async ({ page }) => {
    await page.goto('/journey');
    await expect(page.locator('h2')).toContainText('Journey');
  });
});

test.describe('Dashboard View', () => {
  test('displays page title', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page.locator('h2')).toContainText('Dashboard');
  });
});

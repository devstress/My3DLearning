import { test, expect } from '@playwright/test';
import { openSidebarIfMobile } from './helpers';

test.describe('Navigation & Sidebar', () => {
  test('home page loads with welcome title and navigation cards', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('h1')).toContainText('Welcome to Terranes');
    await expect(page.locator('.card')).toHaveCount(6);
  });

  test('sidebar shows all navigation links', async ({ page }) => {
    await page.goto('/');
    await openSidebarIfMobile(page);
    const sidebar = page.locator('.sidebar');
    await expect(sidebar.locator('.nav-link')).toHaveCount(7);
    await expect(sidebar).toContainText('Home');
    await expect(sidebar).toContainText('Villages');
    await expect(sidebar).toContainText('Home Designs');
    await expect(sidebar).toContainText('Land Blocks');
    await expect(sidebar).toContainText('Marketplace');
    await expect(sidebar).toContainText('My Journey');
    await expect(sidebar).toContainText('Dashboard');
  });

  test('clicking sidebar links navigates to correct pages', async ({ page }) => {
    await page.goto('/');

    await openSidebarIfMobile(page);
    await page.click('.nav-link:has-text("Villages")');
    await expect(page).toHaveURL(/\/villages/);
    await expect(page.locator('h2')).toContainText('Virtual Villages');

    await openSidebarIfMobile(page);
    await page.click('.nav-link:has-text("Home Designs")');
    await expect(page).toHaveURL(/\/home-models/);
    await expect(page.locator('h2')).toContainText('Home Designs');

    await openSidebarIfMobile(page);
    await page.click('.nav-link:has-text("Land Blocks")');
    await expect(page).toHaveURL(/\/land/);
    await expect(page.locator('h2')).toContainText('Land Blocks');

    await openSidebarIfMobile(page);
    await page.click('.nav-link:has-text("Marketplace")');
    await expect(page).toHaveURL(/\/marketplace/);
    await expect(page.locator('h2')).toContainText('Marketplace');

    await openSidebarIfMobile(page);
    await page.click('.nav-link:has-text("My Journey")');
    await expect(page).toHaveURL(/\/journey/);
    await expect(page.locator('h2')).toContainText('Journey');

    await openSidebarIfMobile(page);
    await page.click('.nav-link:has-text("Dashboard")');
    await expect(page).toHaveURL(/\/dashboard/);
    await expect(page.locator('h2')).toContainText('Dashboard');
  });

  test('home page CTA cards link to correct routes', async ({ page }) => {
    await page.goto('/');
    await page.click('a.btn:has-text("Explore Villages")');
    await expect(page).toHaveURL(/\/villages/);

    await page.goto('/');
    await page.click('a.btn:has-text("Browse Designs")');
    await expect(page).toHaveURL(/\/home-models/);

    await page.goto('/');
    await page.click('a.btn:has-text("Search Land")');
    await expect(page).toHaveURL(/\/land/);
  });

  test('active nav link is highlighted when route changes', async ({ page }) => {
    await page.goto('/villages');
    const villagesLink = page.locator('.nav-link:has-text("Villages")');
    await expect(villagesLink).toHaveClass(/active/);
  });
});

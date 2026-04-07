import type { Page } from '@playwright/test';

/**
 * On mobile viewports, the sidebar nav is hidden until toggled.
 * This helper opens the sidebar if the toggler button is visible.
 */
export async function openSidebarIfMobile(page: Page): Promise<void> {
  const toggler = page.locator('.navbar-toggler');
  if (await toggler.isVisible()) {
    await toggler.click();
    // Wait for the nav-scrollable to have .open class
    await page.locator('.nav-scrollable.open').waitFor({ state: 'visible' });
  }
}

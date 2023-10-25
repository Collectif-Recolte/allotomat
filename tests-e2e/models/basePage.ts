import { Locator, Page } from "@playwright/test";

export class BasePage {
  page: Page;
  profileMenu: Locator;
  profileMenuButton: Locator;
  notifications: Locator;

  constructor(page: Page) {
    this.page = page;

    this.profileMenu = page.locator("[data-test-id=profile-menu]");
    this.profileMenuButton = page.locator("[data-test-id=profile-menu-button]");
    this.notifications = page.locator("[data-test-id=notifications]");
  }
}

import { Locator, Page } from "@playwright/test";
import { BasePage } from "./basePage";

export class LoginPage extends BasePage {
  emailField: Locator;
  passwordField: Locator;
  loginButton: Locator;

  constructor(page: Page) {
    super(page);

    this.emailField = page.locator("input#email");
    this.passwordField = page.locator("input#password");
    this.loginButton = page.locator("button[type=submit]");
  }

  async navigate() {
    await this.page.goto("/");
  }

  async login(username: string, password: string) {
    await this.emailField.fill(username);
    await this.passwordField.fill(password);
    await this.loginButton.click();
  }
}

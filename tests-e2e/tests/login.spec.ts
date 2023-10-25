import { test, expect } from "@playwright/test";
import { LoginPage } from "../models/loginPage";

test.describe("login with valid accounts", () => {
  test("login as admin", async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.navigate();
    await loginPage.login("admin1@example.com", "Abcd1234!!");

    await loginPage.profileMenuButton.click();
    await expect(loginPage.profileMenu).toContainText("Admin1 Example");
  });
});

test("login with bad password", async ({ page }) => {
  const loginPage = new LoginPage(page);

  await loginPage.navigate();
  await loginPage.login("admin1@example.com", "WRONG PASSWORD");

  await expect(loginPage.notifications).toHaveText("Le mot de passe ou le courriel n'est pas valide.Fermer la notification");
});

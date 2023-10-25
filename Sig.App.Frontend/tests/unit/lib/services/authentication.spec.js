import { createTestingPinia } from "@pinia/testing";
import "jest-location-mock";

import { Client } from "@/lib/helpers/client";
import { addMessages } from "@/lib/i18n";
import AuthenticationService from "@/lib/services/authentication";
import AuthenticationServiceMessages from "@/lib/services/authentication.i18n.json";
import router from "@/lib/router";
import { useAuthStore } from "@/lib/store/auth";
import { URL_ROOT } from "@/lib/consts/urls";
import { useNotificationsStore } from "@/lib/store/notifications";

jest.mock("@/lib/helpers/client");
jest.mock("@/lib/router");

describe("services/authentication.js", () => {
  beforeEach(() => {
    addMessages(AuthenticationServiceMessages);
    createTestingPinia({ fakeApp: true });

    router.currentRoute.value = { query: {} };
  });
  afterEach(() => {
    jest.clearAllMocks();
  });

  const sampleClaims = {
    claim1: "value1",
    claim2: "value2",
    claim3: "value3"
  };

  const sampleGlobalPermissions = [];

  const claimsAndGlobalPermissions = { claims: sampleClaims, globalPermissions: sampleGlobalPermissions };

  function setupSuccessfulPost() {
    Client.post.mockImplementationOnce(() => ({
      status: 200,
      data: claimsAndGlobalPermissions
    }));
  }

  describe("login", () => {
    it("sets the claims in store on success", async () => {
      setupSuccessfulPost();

      await AuthenticationService.login("username", "password");

      const authStore = useAuthStore();
      expect(authStore.initialize).toHaveBeenCalledWith(sampleClaims, sampleGlobalPermissions);
    });

    it("redirects to default url on success", async () => {
      setupSuccessfulPost();

      await AuthenticationService.login("username", "password");

      expect(router.push).toHaveBeenCalledWith({ name: URL_ROOT });
    });

    it("redirects to returnPath on success", async () => {
      setupSuccessfulPost();
      router.currentRoute.value.query.returnPath = "return-path";

      await AuthenticationService.login("username", "password");

      expect(router.push).toHaveBeenCalledWith("return-path");
    });

    it("shows an error when api call fails", async () => {
      Client.post.mockImplementationOnce(() => Promise.reject());

      await AuthenticationService.login("username", "password");

      const notificationsStore = useNotificationsStore();
      expect(notificationsStore.addError).toHaveBeenCalled();
    });
  });

  describe("logout", () => {
    beforeEach(() => {
      window.location.assign("/some-page");
    });

    it("calls logout on the backend", async () => {
      await AuthenticationService.logout();

      expect(Client.post).toHaveBeenCalledWith("/account/logout");
    });

    it("redirects to root url", async () => {
      await AuthenticationService.logout();
      expect(window.location).toBeAt("/login");
    });

    it("clears the claims from the store", async () => {
      await AuthenticationService.logout();

      const authStore = useAuthStore();
      expect(authStore.initialize).toHaveBeenCalledWith(null, []);
    });
  });

  describe("refresh", () => {
    it("gets updated claims from the server", async () => {
      await AuthenticationService.refresh();

      expect(Client.get).toHaveBeenCalledWith("/account/refresh");
    });

    it("sets the new claims in the store", async () => {
      Client.get.mockImplementationOnce(() => ({
        status: 200,
        data: claimsAndGlobalPermissions
      }));

      await AuthenticationService.refresh();

      const authStore = useAuthStore();
      expect(authStore.initialize).toHaveBeenCalledWith(sampleClaims, sampleGlobalPermissions);
    });

    it("clears the claims from the store on non-success code", async () => {
      Client.get.mockImplementationOnce(() => ({
        status: 401
      }));

      await AuthenticationService.refresh();

      const authStore = useAuthStore();
      expect(authStore.initialize).toHaveBeenCalledWith(null, []);
    });

    it("clears the claims from the store on error", async () => {
      Client.get.mockImplementationOnce(() => Promise.reject());

      await AuthenticationService.refresh();

      const authStore = useAuthStore();
      expect(authStore.initialize).toHaveBeenCalledWith(null, []);
    });

    it("does not navigate on error if user is not logged in", async () => {
      window.location.assign("/some-page");

      Client.get.mockImplementationOnce(() => Promise.reject());

      await AuthenticationService.refresh();

      expect(window.location).toBeAt("/some-page");
    });

    it("navigates to login on error if user was logged in", async () => {
      window.location.assign("/some-page");

      const authStore = useAuthStore();
      authStore.claims = {};
      authStore.globalPermissions = sampleGlobalPermissions;

      Client.get.mockImplementationOnce(() => Promise.reject());

      await AuthenticationService.refresh();

      expect(window.location).toBeAt("/");
    });
  });
});

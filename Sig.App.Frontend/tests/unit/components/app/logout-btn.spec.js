import { shallowMount } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";
import LogoutBtn from "@/components/app/logout-btn";
import i18n from "@/lib/i18n";
import AuthenticationService from "@/lib/services/authentication";
import { useAuthStore } from "@/lib/store/auth";

jest.mock("@/lib/services/authentication");

describe("logout-btn.vue", () => {
  let mountOptions;
  const sampleGlobalPermissions = [];

  beforeEach(() => {
    mountOptions = {
      global: {
        plugins: [i18n, createTestingPinia()]
      },
      components: {
        PfButtonAction: {}
      }
    };
  });

  it("is shown when logged in", async () => {
    const authStore = useAuthStore();
    authStore.claims = {};
    authStore.globalPermissions = sampleGlobalPermissions;

    const wrapper = shallowMount(LogoutBtn, mountOptions);

    expect(wrapper.find("[data-test-id=logout]").exists()).toBeTruthy();
  });

  it("hides the logout button when not logged in", async () => {
    const wrapper = shallowMount(LogoutBtn, mountOptions);

    expect(wrapper.find("[data-test-id=logout]").exists()).toBeFalsy();
  });

  it("logs out the user when clicking the logout button", async () => {
    const authStore = useAuthStore();
    authStore.claims = {};
    authStore.globalPermissions = sampleGlobalPermissions;

    const wrapper = shallowMount(LogoutBtn, mountOptions);

    wrapper.find("[data-test-id=logout]").trigger("click");

    expect(AuthenticationService.logout).toHaveBeenCalled();
  });
});

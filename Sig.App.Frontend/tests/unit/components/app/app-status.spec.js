import { flushPromises, shallowMount } from "@vue/test-utils";
import "jest-location-mock";

import AppStatus from "@/components/app/app-status";
import i18n from "@/lib/i18n";
import { Client } from "@/lib/helpers/client";

jest.mock("@/lib/helpers/client");

describe("app-status.vue", () => {
  let mountOptions;
  let wrapper;

  beforeEach(() => {
    mountOptions = {
      global: {
        plugins: [i18n]
      },
      components: {
        PfButtonAction: {}
      }
    };
  });

  afterEach(() => {
    jest.resetAllMocks();
    wrapper.unmount();
  });

  it("does not show anything when the status is ready", async () => {
    Client.get.mockImplementationOnce(() => ({
      data: {
        ready: true,
        version: "1"
      }
    }));

    wrapper = shallowMount(AppStatus, mountOptions);

    await flushPromises();

    expect(wrapper.text()).toEqual("");
  });

  it("shows a warning when the status is not ready", async () => {
    Client.get.mockImplementationOnce(() => ({
      data: {
        ready: false,
        version: "1"
      }
    }));

    wrapper = shallowMount(AppStatus, mountOptions);

    await flushPromises();

    expect(wrapper.text()).toContain("Désolé, l'application rencontre présentement des problèmes.");
  });

  it("shows a warning when the status resolves with an error", async () => {
    Client.get.mockImplementationOnce(() => Promise.reject("Fake error"));

    wrapper = shallowMount(AppStatus, mountOptions);

    await flushPromises();

    expect(wrapper.text()).toContain("Désolé, l'application rencontre présentement des problèmes.");
  });

  it("shows a message when the app becomes out of date", async () => {
    jest.useFakeTimers();

    Client.get.mockImplementationOnce(() => ({
      data: {
        ready: true,
        version: "1"
      }
    }));

    wrapper = shallowMount(AppStatus, mountOptions);
    await flushPromises();

    Client.get.mockImplementationOnce(() => ({
      data: {
        ready: true,
        version: "2"
      }
    }));

    jest.runOnlyPendingTimers();
    await flushPromises();

    expect(wrapper.text()).toContain("Une nouvelle version de l'application est disponible.");
    expect(wrapper.find("[data-test-id=refresh]").exists()).toBeTruthy();
  });

  it("does not show anything when the version stays the same", async () => {
    jest.useFakeTimers();

    Client.get.mockImplementation(() => ({
      data: {
        ready: true,
        version: "1"
      }
    }));

    wrapper = shallowMount(AppStatus, mountOptions);
    await flushPromises();

    jest.runOnlyPendingTimers();
    await flushPromises();

    expect(wrapper.text()).toEqual("");
  });

  it("reloads the page when the user clicks the button", async () => {
    jest.useFakeTimers();

    Client.get.mockImplementationOnce(() => ({
      data: {
        ready: true,
        version: "1"
      }
    }));

    wrapper = shallowMount(AppStatus, mountOptions);
    await flushPromises();

    Client.get.mockImplementationOnce(() => ({
      data: {
        ready: true,
        version: "2"
      }
    }));

    jest.runOnlyPendingTimers();
    await flushPromises();

    wrapper.find("[data-test-id=refresh]").trigger("click");
    expect(window.location.reload).toHaveBeenCalled();
  });
});

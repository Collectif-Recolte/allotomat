import { shallowMount } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";

import { ConstsPlugin } from "@/lib/consts";
import i18n from "@/lib/i18n";
import NotFound from "@/views/NotFound.vue";

describe("NotFound.vue", () => {
  let mountOptions;

  beforeEach(() => {
    mountOptions = {
      global: {
        plugins: [i18n, createTestingPinia(), ConstsPlugin],
        stubs: {
          RouterLink: true
        }
      }
    };
  });

  it("shows a not-found message in english by default", async () => {
    const wrapper = shallowMount(NotFound, mountOptions);

    expect(wrapper.find("h1").text()).toEqual("Page not found");
  });

  it("shows a not-found message in french", async () => {
    i18n.global.locale.value = "fr";

    const wrapper = shallowMount(NotFound, mountOptions);

    expect(wrapper.find("h1").text()).toEqual("Page introuvable");
  });
});

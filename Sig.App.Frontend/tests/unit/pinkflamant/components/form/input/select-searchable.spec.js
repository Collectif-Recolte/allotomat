import { mount, flushPromises } from "@vue/test-utils";
import { createI18n } from "vue-i18n";

import SelectSearchable from "@/../pinkflamant/components/form/input/select-searchable.vue";

const i18n = createI18n({
  legacy: false,
  locale: "fr",
  messages: {
    fr: {},
    en: {}
  }
});

const options = [
  { label: "École primaire", value: "org-1" },
  { label: "Garderie", value: "org-2" },
  { label: "CPE", value: "org-3" }
];

function mountSelectSearchable(props = {}) {
  return mount(SelectSearchable, {
    props: {
      id: "test-select",
      label: "Groupe",
      options,
      ...props
    },
    global: {
      plugins: [i18n],
      stubs: {
        PfIcon: true
      }
    }
  });
}

describe("select-searchable.vue", () => {
  it("emits input when an option is selected", async () => {
    const wrapper = mountSelectSearchable();
    const input = wrapper.find('input[role="combobox"]');

    await input.trigger("focus");
    await input.setValue("école");
    await flushPromises();

    const option = wrapper.findAll("li").find((node) => node.text().includes("École primaire"));
    expect(option).toBeTruthy();
    await option.trigger("mousedown");
    await option.trigger("click");
    await flushPromises();

    expect(wrapper.emitted("input")).toBeTruthy();
    expect(wrapper.emitted("input")[0]).toEqual(["org-1"]);
  });

  it("does not emit input when query is cleared without selecting", async () => {
    const wrapper = mountSelectSearchable({ value: "org-1" });
    const input = wrapper.find('input[role="combobox"]');

    await input.trigger("focus");
    await input.setValue("inexistant");
    await flushPromises();
    await input.trigger("blur");
    await flushPromises();

    expect(wrapper.emitted("input")).toBeFalsy();
  });

  it("clears the query with the clear button without emitting", async () => {
    const wrapper = mountSelectSearchable({ value: "org-1" });
    const input = wrapper.find('input[role="combobox"]');

    await input.trigger("focus");
    await input.setValue("école");
    await flushPromises();

    const clearButton = wrapper.find('button[aria-label="Effacer la recherche"]');
    expect(clearButton.exists()).toBe(true);
    await clearButton.trigger("click");
    await flushPromises();

    expect(wrapper.emitted("input")).toBeFalsy();
    expect(wrapper.vm.query).toBe("");
  });

  it("emits input when selecting with Enter key", async () => {
    const wrapper = mountSelectSearchable();
    const input = wrapper.find('input[role="combobox"]');

    await input.trigger("focus");
    await input.setValue("école");
    await flushPromises();

    await input.trigger("keydown", { key: "Enter" });
    await flushPromises();

    expect(wrapper.emitted("input")).toBeTruthy();
    expect(wrapper.emitted("input")[0]).toEqual(["org-1"]);
  });

  it("does not emit the first option on blur when a value is already selected", async () => {
    const allGroupsOptions = [
      { label: "Tous les groupes", value: "all-group" },
      { label: "École primaire", value: "org-1" }
    ];
    const wrapper = mountSelectSearchable({ value: "org-1", options: allGroupsOptions });
    const input = wrapper.find('input[role="combobox"]');

    await input.trigger("focus");
    await flushPromises();
    await input.trigger("blur");
    await flushPromises();

    expect(wrapper.emitted("input")).toBeFalsy();
  });
});

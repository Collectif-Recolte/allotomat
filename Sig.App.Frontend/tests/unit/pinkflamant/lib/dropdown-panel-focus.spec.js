import { defineComponent, h } from "vue";
import { flushPromises, mount } from "@vue/test-utils";

import {
  provideDropdownPanelFocus,
  useDropdownPanelFocus
} from "@/../pinkflamant/lib/dropdown-panel-focus";

const Consumer = defineComponent({
  name: "DropdownPanelFocusConsumer",
  props: {
    onFocus: {
      type: Function,
      required: true
    }
  },
  setup(props) {
    useDropdownPanelFocus(props.onFocus);
    return () => h("div");
  }
});

const Provider = defineComponent({
  name: "DropdownPanelFocusProvider",
  setup(_, { slots }) {
    const { requestContentFocus } = provideDropdownPanelFocus();
    return () =>
      h("div", [
        h("button", { onFocus: requestContentFocus, onMousedown: requestContentFocus }, "open"),
        slots.default?.()
      ]);
  }
});

describe("dropdown-panel-focus.js", () => {
  it("calls the registered handler when requestContentFocus is triggered", async () => {
    const onFocus = jest.fn();

    const wrapper = mount(Provider, {
      slots: {
        default: () => h(Consumer, { onFocus })
      }
    });

    await wrapper.find("button").trigger("focus");
    await flushPromises();

    expect(onFocus).toHaveBeenCalledTimes(1);
  });

  it("does nothing when requestContentFocus is triggered without a registered handler", async () => {
    const wrapper = mount(Provider);

    await expect(wrapper.find("button").trigger("focus")).resolves.not.toThrow();
    await flushPromises();
  });

  it("does not call the handler after the consumer unmounts", async () => {
    const onFocus = jest.fn();
    let showConsumer = true;

    const wrapper = mount(
      defineComponent({
        setup() {
          const { requestContentFocus } = provideDropdownPanelFocus();
          return () =>
            h("div", [
              h("button", { onFocus: requestContentFocus }, "open"),
              showConsumer ? h(Consumer, { onFocus }) : null
            ]);
        }
      })
    );

    await wrapper.find("button").trigger("focus");
    await flushPromises();
    expect(onFocus).toHaveBeenCalledTimes(1);

    showConsumer = false;
    await wrapper.vm.$forceUpdate();
    await flushPromises();

    onFocus.mockClear();
    await wrapper.find("button").trigger("focus");
    await flushPromises();

    expect(onFocus).not.toHaveBeenCalled();
  });

  it("allows mounting a consumer without a provider", () => {
    const onFocus = jest.fn();

    expect(() =>
      mount(Consumer, {
        props: { onFocus }
      })
    ).not.toThrow();
  });
});

import { config, shallowMount } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";
import i18n from "@/lib/i18n";

import NotificationAlert from "@/components/notifications/notification-alert";
import { NOTIFICATION_TYPE_INFO } from "@/lib/consts/notifications";
import { useNotificationsStore } from "@/lib/store/notifications";

describe("notification-alert.vue", () => {
  const testNotification = { id: "N1", type: NOTIFICATION_TYPE_INFO, text: "First notification", dismissible: true };
  let mountOptions;

  beforeEach(() => {
    config.renderStubDefaultSlot = true;

    mountOptions = {
      global: {
        plugins: [createTestingPinia(), i18n],
        stubs: {
          PfAlert: true
        }
      }
    };
  });

  it("shows the notification text", async () => {
    const wrapper = shallowMount(NotificationAlert, { ...mountOptions, props: { item: testNotification } });

    expect(wrapper.text()).toContain("First notification");
  });

  it("dismisses the notification when triggered", async () => {
    const wrapper = shallowMount(NotificationAlert, { ...mountOptions, props: { item: testNotification } });
    const notificationsStore = useNotificationsStore();

    const alert = wrapper.findComponent({ name: "PfAlert" });
    alert.vm.$emit("dismiss");

    expect(notificationsStore.dismiss).toHaveBeenCalledWith("N1");
  });
});

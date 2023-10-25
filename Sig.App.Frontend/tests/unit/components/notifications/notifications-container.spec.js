import { shallowMount } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";

import NotificationsContainer from "@/components/notifications/notifications-container";
import NotificationAlert from "@/components/notifications/notification-alert";
import { NOTIFICATION_TYPE_INFO } from "@/lib/consts/notifications";
import { useNotificationsStore } from "@/lib/store/notifications";

describe("notifications.container.vue", () => {
  const testNotifications = [
    { id: "N1", type: NOTIFICATION_TYPE_INFO, text: "First notification", dismissible: true },
    { id: "N2", type: NOTIFICATION_TYPE_INFO, text: "Second notification", dismissible: true },
    { id: "N3", type: NOTIFICATION_TYPE_INFO, text: "Third notification", dismissible: true },
    { id: "N4", type: NOTIFICATION_TYPE_INFO, text: "Fourth notification", dismissible: true }
  ];

  let mountOptions;

  beforeEach(() => {
    mountOptions = {
      global: {
        plugins: [createTestingPinia()]
      }
    };
  });

  it("shows a notification-alert for each notification", async () => {
    const notificationsStore = useNotificationsStore();
    notificationsStore.notifications = testNotifications;

    const wrapper = shallowMount(NotificationsContainer, mountOptions);

    const alerts = wrapper.findAllComponents(NotificationAlert);
    expect(alerts).toHaveLength(4);

    for (const index in testNotifications) {
      const alert = alerts[index];
      const notification = testNotifications[index];

      expect(alert.props()).toHaveProperty("item", notification);
    }
  });
});

import { setActivePinia, createPinia } from "pinia";
import { useNotificationsStore } from "@/lib/store/notifications";
import {
  NOTIFICATION_TYPE_ERROR,
  NOTIFICATION_TYPE_INFO,
  NOTIFICATION_TYPE_SUCCESS,
  NOTIFICATION_TYPE_WARNING
} from "@/lib/consts/notifications";

describe("store/notifications.js", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("adds notifications", () => {
    const notificationsStore = useNotificationsStore();

    notificationsStore.addSuccess("Success");
    notificationsStore.addInfo("Info");
    notificationsStore.addWarning("Warning");
    notificationsStore.addError("Error");

    expect(notificationsStore.notifications).toHaveLength(4);
    expect(notificationsStore.notifications[0]).toMatchObject({ type: NOTIFICATION_TYPE_SUCCESS, text: "Success" });
    expect(notificationsStore.notifications[1]).toMatchObject({ type: NOTIFICATION_TYPE_INFO, text: "Info" });
    expect(notificationsStore.notifications[2]).toMatchObject({ type: NOTIFICATION_TYPE_WARNING, text: "Warning" });
    expect(notificationsStore.notifications[3]).toMatchObject({ type: NOTIFICATION_TYPE_ERROR, text: "Error" });
  });

  it("removes notifications when they are dismissed", () => {
    const notificationsStore = useNotificationsStore();

    notificationsStore.addSuccess("Test 1");
    notificationsStore.addSuccess("Test 2");

    notificationsStore.dismiss(notificationsStore.notifications[0].id);
    expect(notificationsStore.notifications).toHaveLength(1);
    expect(notificationsStore.notifications[0].text).toEqual("Test 2");

    notificationsStore.dismiss(notificationsStore.notifications[0].id);
    expect(notificationsStore.notifications).toHaveLength(0);
  });

  it("removes notifications after they expire", () => {
    jest.useFakeTimers();
    const notificationsStore = useNotificationsStore();

    notificationsStore.addSuccess("Test", 2000);

    jest.advanceTimersByTime(1500);
    expect(notificationsStore.notifications).toHaveLength(1);

    jest.advanceTimersByTime(500);
    expect(notificationsStore.notifications).toHaveLength(0);
  });
});

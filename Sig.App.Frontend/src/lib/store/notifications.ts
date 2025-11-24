import { defineStore } from "pinia";

import {
  NOTIFICATION_TYPE_SUCCESS,
  NOTIFICATION_TYPE_INFO,
  NOTIFICATION_TYPE_WARNING,
  NOTIFICATION_TYPE_ERROR
} from "@/lib/consts/notifications";
import { uniqueId } from "@/lib/helpers/id-helpers";

type NotificationType =
  | typeof NOTIFICATION_TYPE_SUCCESS
  | typeof NOTIFICATION_TYPE_INFO
  | typeof NOTIFICATION_TYPE_WARNING
  | typeof NOTIFICATION_TYPE_ERROR;

interface Notification {
  id: string;
  type: NotificationType;
  text: string;
  duration: number;
  dismissible: boolean;
}

export const useNotificationsStore = defineStore("notifications", {
  state: () => ({
    notifications: [] as Notification[]
  }),
  actions: {
    addSuccess(text: string, duration = 10000, dismissible = true) {
      this.add(NOTIFICATION_TYPE_SUCCESS, text, dismissible, duration);
    },
    addInfo(text: string, duration = 10000, dismissible = true) {
      this.add(NOTIFICATION_TYPE_INFO, text, dismissible, duration);
    },
    addWarning(text: string, duration = 10000, dismissible = true) {
      this.add(NOTIFICATION_TYPE_WARNING, text, dismissible, duration);
    },
    addError(text: string, duration = 10000, dismissible = true) {
      this.add(NOTIFICATION_TYPE_ERROR, text, dismissible, duration);
    },
    add(type: NotificationType, text: string, dismissible: boolean, duration: number) {
      const notification = {
        id: uniqueId(),
        type,
        text,
        duration,
        dismissible
      };

      // @ts-ignore
      this.notifications.push(notification);

      if (duration > 0) {
        setTimeout(() => {
          this.dismiss(notification.id);
        }, duration);
      }

      return notification.id;
    },
    dismiss(id: string) {
      // @ts-ignore
      this.notifications = this.notifications.filter((n) => n.id !== id);
    }
  }
});

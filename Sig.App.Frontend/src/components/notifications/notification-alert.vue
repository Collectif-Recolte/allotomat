<i18n>
{
	"en": {
		"close-notification": "Close the notification"
	},
	"fr": {
		"close-notification": "Fermer la notification"
	}
}
</i18n>

<template>
  <PfAlert
    class="mb-1"
    :color-theme="displayType"
    :close-alert-label="t('close-notification')"
    @dismiss="dismiss(props.item.id)"
    >{{ item.text }}</PfAlert
  >
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { computed, defineProps } from "vue";
import { useNotificationsStore } from "@/lib/store/notifications";

import {
  NOTIFICATION_TYPE_SUCCESS,
  NOTIFICATION_TYPE_WARNING,
  NOTIFICATION_TYPE_ERROR,
  NOTIFICATION_TYPE_INFO
} from "@/lib/consts/notifications";

const { t } = useI18n();

const { dismiss } = useNotificationsStore();

const props = defineProps({
  item: {
    type: Object,
    required: true
  }
});

const displayType = computed(() => {
  switch (props.item.type) {
    case NOTIFICATION_TYPE_SUCCESS: {
      return {
        bg: "bg-yellow-500 dark:bg-grey-800",
        text: "text-green-800 dark:text-green-300",
        accent: "text-green-800 dark:text-green-400"
      };
    }
    case NOTIFICATION_TYPE_WARNING: {
      return {
        bg: "bg-red-50 dark:bg-grey-800",
        text: "text-red-700 dark:text-red-300",
        accent: "text-red-500 dark:text-red-400"
      };
    }
    case NOTIFICATION_TYPE_ERROR: {
      return {
        bg: "bg-red-50 dark:bg-grey-800",
        text: "text-red-700 dark:text-red-300",
        accent: "text-red-500 dark:text-red-400"
      };
    }
    case NOTIFICATION_TYPE_INFO:
    default: {
      return {
        bg: "bg-yellow-500 dark:bg-grey-800",
        text: "text-primary-900 dark:text-primary-300",
        accent: "text-primary-900 dark:text-primary-500"
      };
    }
  }
});
</script>

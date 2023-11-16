<template>
  <div v-if="notifications?.length > 0" class="fixed inset-x-2 top-2 z-40 sm:w-xs sm:left-auto" data-test-id="notifications">
    <NotificationAlert v-for="notification in notifications" :key="notification.id" :item="notification" />
  </div>
</template>

<script setup>
import { storeToRefs } from "pinia";

import NotificationAlert from "./notification-alert";
import { useNotificationsStore } from "@/lib/store/notifications";

const { notifications } = storeToRefs(useNotificationsStore());
</script>

<template>
  <AppStatus />
  <NotificationContainer />
  <RouterView />
</template>

<script setup>
import NotificationContainer from "@/components/notifications/notifications-container";
import AppStatus from "@/components/app/app-status.vue";
import AuthenticationService from "@/lib/services/authentication";

const AUTH_REFRESH_INTERVAL = 60_000;
setInterval(() => AuthenticationService.refresh(), AUTH_REFRESH_INTERVAL);

AuthenticationService.refresh();
</script>

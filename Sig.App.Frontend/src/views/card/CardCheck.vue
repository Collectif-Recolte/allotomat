<i18n>
  {
    "en": {
      "title": "Check card balance"
    },
    "fr": {
      "title": "Vérifier le solde d'une carte"
    }
  }
  </i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell
      :class="appShellClass"
      :is-dark="isDark"
      :loading="loading"
      :no-padding="isDark"
      hide-profile-menu
      :title="isDark ? null : t('title')">
      <NewCardCheck v-if="activeStep === CHECK_CARD_STEPS_START" @onUpdateStep="updateStep" />
      <ScanQRCode v-else-if="activeStep === CHECK_CARD_STEPS_SCAN" @onUpdateStep="updateStep" />
      <Balance
        v-else-if="activeStep === CHECK_CARD_STEPS_COMPLETE"
        :card-id="cardId"
        @onUpdateStep="updateStep"
        @onUpdateLoadingState="updateLoadingState" />
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useRoute, onBeforeRouteUpdate } from "vue-router";

import ScanQRCode from "@/views/card/ScanQRCode";
import Balance from "@/views/card/Balance";
import NewCardCheck from "@/views/card/NewCardCheck";

import { CHECK_CARD_STEPS_START, CHECK_CARD_STEPS_SCAN, CHECK_CARD_STEPS_COMPLETE } from "@/lib/consts/enums";

const route = useRoute();
const { t } = useI18n();

usePageTitle(t("title"));

const activeStep = ref(route.query.isScan ? CHECK_CARD_STEPS_SCAN : CHECK_CARD_STEPS_START);
const cardId = ref("");
const loading = ref(false);

const isDark = computed(() => (activeStep.value === CHECK_CARD_STEPS_COMPLETE ? true : false));

const appShellClass = computed(() => {
  if (isDark.value) return "bg-primary-700";
  return "bg-primary-100 md:bg-white";
});

const updateStep = (stepName, cardIdString) => {
  activeStep.value = stepName;
  if (cardIdString) cardId.value = cardIdString;
  if (stepName === CHECK_CARD_STEPS_COMPLETE) loading.value = false;
};

const updateLoadingState = (state) => {
  loading.value = state;
};

onBeforeRouteUpdate((to) => {
  if (to.query.isScan) {
    activeStep.value = CHECK_CARD_STEPS_SCAN;
  }
});
</script>

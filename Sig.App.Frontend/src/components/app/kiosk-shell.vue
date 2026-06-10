<i18n>
{
  "en": {
    "cancel": "Cancel",
    "logo": "Tomat logo"
  },
  "fr": {
    "cancel": "Annuler",
    "logo": "Logo de Tomat"
  }
}
</i18n>

<template>
  <div class="kiosk-mode min-h-[100dvh] flex flex-col bg-primary-100">
    <header class="sticky top-0 z-30 bg-primary-700 px-4 py-3 flex items-center justify-between gap-4">
      <img class="h-8" :src="require('@/assets/logo/logo-white.svg')" :alt="t('logo')" />
      <div class="flex items-center gap-3">
        <PfButtonAction
          v-if="showCancel"
          btn-style="secondary"
          size="lg"
          :label="t('cancel')"
          @click="emit('cancel')" />
        <KioskLangSwitch />
      </div>
    </header>
    <main class="flex-1 flex flex-col">
      <Loading :loading="loading" is-full-height>
        <slot />
      </Loading>
    </main>
  </div>
</template>

<script setup>
import { defineEmits, defineProps } from "vue";
import { useI18n } from "vue-i18n";

import Loading from "@/components/app/loading";
import KioskLangSwitch from "@/components/app/kiosk-lang-switch";

const { t } = useI18n();

defineProps({
  loading: Boolean,
  showCancel: { type: Boolean, default: false }
});

const emit = defineEmits(["cancel"]);
</script>

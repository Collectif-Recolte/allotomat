<i18n>
  {
    "en": {
      "title": "Create a transaction",
      "start-transaction": "Scan a card"
    },
    "fr": {
      "title": "Créer une transaction",
      "start-transaction": "Scanner une carte"
    }
  }
  </i18n>

<template>
  <UiEmptyPage class="min-h-app px-8">
    <UiCta
      :img-src="require('@/assets/img/scan-marchand.jpg')"
      :primary-btn-label="t('start-transaction')"
      primary-btn-is-action
      @onPrimaryBtnClick="emit('onUpdateStep', TRANSACTION_STEPS_SCAN)" />
  </UiEmptyPage>
</template>

<script setup>
import { defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { TRANSACTION_STEPS_SCAN } from "@/lib/consts/enums";

const { t } = useI18n();
usePageTitle(t("title"));

const emit = defineEmits(["onUpdateStep"]);
</script>

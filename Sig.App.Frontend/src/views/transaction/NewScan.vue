<i18n>
  {
    "en": {
      "title": "Create a transaction",
      "start-transaction": "Scan a card",
      "manually-enter-card-number": "Manually enter card number"
    },
    "fr": {
      "title": "Créer une transaction",
      "start-transaction": "Scanner une carte",
      "manually-enter-card-number": "Saisir le numéro de la carte"
    }
  }
  </i18n>

<template>
  <UiEmptyPage class="min-h-app px-8">
    <UiCta
      :img-src="require('@/assets/img/scan-marchand.jpg')"
      :primary-btn-label="t('start-transaction')"
      :secondary-btn-label="t('manually-enter-card-number')"
      primary-btn-is-action
      secondary-btn-is-action
      @onPrimaryBtnClick="emit('onUpdateStep', TRANSACTION_STEPS_SCAN, {})"
      @onSecondaryBtnClick="emit('onUpdateStep', TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER, {})" />
  </UiEmptyPage>
</template>

<script setup>
import { defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { TRANSACTION_STEPS_SCAN, TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER } from "@/lib/consts/enums";

const { t } = useI18n();
usePageTitle(t("title"));

const emit = defineEmits(["onUpdateStep"]);
</script>

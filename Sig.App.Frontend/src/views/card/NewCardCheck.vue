<i18n>
  {
    "en": {
      "title": "Check card balance",
      "restart-scan": "Scan a card",
      "camera-access": "You must have access to a camera."
    },
    "fr": {
      "title": "Vérifier le solde d'une carte",
      "restart-scan": "Scanner une carte",
      "camera-access": "Vous devez avoir accès à une caméra."
    }
  }
  </i18n>

<template>
  <UiEmptyPage>
    <UiCta
      :img-src="require('@/assets/img/scan-marchand.jpg')"
      :primary-btn-label="t('restart-scan')"
      :primary-btn-icon="ICON_SCAN"
      primary-btn-is-action
      :description="t('camera-access')"
      @onPrimaryBtnClick="emit('onUpdateStep', CHECK_CARD_STEPS_SCAN)" />
  </UiEmptyPage>
</template>

<script setup>
import { defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";

import ICON_SCAN from "@/lib/icons/scan.json";
import { CHECK_CARD_STEPS_SCAN } from "@/lib/consts/enums";

const { t } = useI18n();

usePageTitle(t("title"));

const emit = defineEmits(["onUpdateStep"]);
</script>

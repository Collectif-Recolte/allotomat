<i18n>
  {
    "en": {
      "dialog-modal-description": "Phasellus magna. Quisque malesuada placerat nisl. Ut varius tincidunt libero.",
      "dialog-modal-title": "Important informations",
      "title": "Basic dialog"
    },
    "fr": {
      "dialog-modal-description": "Phasellus magna. Quisque malesuada placerat nisl. Ut varius tincidunt libero.",
      "dialog-modal-title": "Informations importantes",
      "title": "Dialogue de base"
    }
  }
</i18n>

<template>
  <UiDialogModal
    :return-route="{ name: URL_DOC_DIALOG }"
    :title="t('dialog-modal-title')"
    :description="t('dialog-modal-description')" />
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { URL_DOC_DIALOG } from "@/lib/consts/urls";

const { t } = useI18n();

usePageTitle(t("title"));
</script>

<i18n>
  {
    "en": {
      "confirm-modal-description": "This action cannot be undone.",
      "confirm-modal-title": "Are you sure you want to proceed?",
      "title": "Examples of dialogs"
    },
    "fr": {
      "confirm-modal-description": "Cette action ne peut être annulée.",
      "confirm-modal-title": "Voulez-vous vraiment continuer?",
      "title": "Exemples de dialogues"
    }
  }
</i18n>

<template>
  <UiDialogConfirmModal
    :return-route="{ name: URL_DOC_DIALOG }"
    :title="t('confirm-modal-title')"
    :description="t('confirm-modal-description')"
    @confirm="onConfirm" />
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { URL_DOC_DIALOG } from "@/lib/consts/urls";

const { t } = useI18n();

usePageTitle(t("title"));

function onConfirm() {
  console.log("C'est confirmé!"); // eslint-disable-line
}
</script>

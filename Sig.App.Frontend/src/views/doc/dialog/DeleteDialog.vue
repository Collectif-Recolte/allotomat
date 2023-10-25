<i18n>
  {
    "en": {
      "delete-text-error": "The text must match the name of the subscription",
      "delete-text-label": "Type the name of the subscription to confirm",
      "element-to-delete": "Element name",
      "description": "Warning ! The deletion of <strong>{elementToDelete}</strong> cannot be undone. If you continue, the subscription will be deleted along with all the elements it contains. Transfers on the cards can no longer be traced. However, the funds of the cards will remain unchanged.",
      "title": "Delete - {elementToDelete}"
    },
    "fr": {
      "delete-text-error": "The text must match the name of the subscription",
      "delete-text-label": "Type the name of the subscription to confirm",
      "element-to-delete": "Nom de l'élément",
      "description": "Avertissement ! La suppression de <strong>{elementToDelete}</strong> ne peut pas être annulée. Si vous continuez, l'abonnement sera supprimé ainsi que tous les éléments qu'il contient de façon définitive. Les transferts sur les cartes ne pourront plus être retracés. Par contre, les fonds des cartes vont rester inchangés.",
      "title": "Supprimer - {elementToDelete}"
    }
  }
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_DOC_DIALOG }"
    :title="t('title', { elementToDelete: getElementToDelete() })"
    :description="t('description', { elementToDelete: getElementToDelete() })"
    :validation-text="getElementToDelete()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="onDelete" />
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { URL_DOC_DIALOG } from "@/lib/consts/urls";

const { t } = useI18n();

usePageTitle(t("title"));

function getElementToDelete() {
  return t("element-to-delete");
}

function onDelete() {
  console.log("C'est supprimé!"); // eslint-disable-line
}
</script>

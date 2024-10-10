<i18n>
  {
    "en": {
      "close": "Close",
      "add-beneficiary": "Add participant",
      "set-subscription": "Assign subscriptions",
      "set-card": "Assign card",
      "set-missed-payment": "Add missed payment", 
      "next-step-btn-add-beneficiary": "Add participant and assign subscriptions",
      "next-step-btn-add-subscription": "Add subscription(s) and assign card",
      "add-beneficiary-success-notification": "Adding {firstname} {lastname} was successful.",
      "add-subscriptions-success-notification": "Adding subscriptions was successful.",
      "title": "Add a participant",
      "set-beneficiary": "Set participant information",
      "id1-already-exist": "ID1 already exists."
    },
    "fr": {
      "close": "Fermer",
      "add-beneficiary": "Ajouter le-la participant-e",
      "set-subscription": "Assigner les abonnements",
      "set-card": "Définir la carte",
      "set-missed-payment": "Ajouter un paiement manqué",    
      "next-step-btn-add-beneficiary": "Ajouter le-la participant-e et assigner des abonnements",
      "next-step-btn-add-subscription": "Assigner les abonnements et assigner une carte",
      "add-beneficiary-success-notification": "L’ajout de {firstname} {lastname} a été un succès.",
      "add-subscriptions-success-notification": "L’ajout des abonnements a été un succès.",
      "title": "Ajouter une caisse",
      "set-beneficiary": "Définir les informations du participant-e",
      "id1-already-exist": "Le numéro d'identification 1 existe déjà."
    }
  }
</i18n>

<template>
  <UiDialogModal :title="t('title')" :has-footer="false" :return-route="{ name: URL_CASH_REGISTER }"> </UiDialogModal>
</template>

<script setup>
import { useI18n } from "vue-i18n";

import { URL_CASH_REGISTER } from "@/lib/consts/urls";

const { t } = useI18n();
</script>

<template>
  <UiDialogModal :return-route="{ name: URL_CARDS_SUMMARY }">
    <QrCodePreview v-if="qrCode" :qr-code="qrCode" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useRoute } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";

import QrCodePreview from "@/components/card/qr-code-preview.vue";

import { URL_CARDS_SUMMARY } from "@/lib/consts/urls";

const route = useRoute();

const { result } = useQuery(
  gql`
    query Card($id: ID!) {
      card(id: $id) {
        id
        qrCode
      }
    }
  `,
  {
    id: route.params.cardId
  }
);
const qrCode = useResult(result, null, (data) => {
  return data.card.qrCode;
});
</script>

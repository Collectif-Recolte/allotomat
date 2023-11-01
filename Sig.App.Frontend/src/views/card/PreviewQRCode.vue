<template>
  <UiDialogModal :return-route="returnRoute()">
    <QrCodePreview v-if="qrCode" :qr-code="qrCode" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useRoute } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";

import QrCodePreview from "@/components/card/qr-code-preview.vue";

import { URL_CARDS_SUMMARY, URL_BENEFICIARY_ADMIN, URL_CARDS_QRCODE_PREVIEW } from "@/lib/consts/urls";

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

function returnRoute() {
  if (route.name === URL_CARDS_QRCODE_PREVIEW) return { name: URL_CARDS_SUMMARY };
  else return { name: URL_BENEFICIARY_ADMIN };
}
</script>

<i18n>
{
	"en": {
		"title": "Assign card - {beneficiaryName}"
	},
	"fr": {
		"title": "Assigner une carte - {beneficiaryName}"
  }
}
</i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :return-route="{ name: URL_BENEFICIARY_ADMIN }"
    :title="t('title', { beneficiaryName: getBeneficiaryName })"
    :has-footer="cardAssignSuccess">
    <AssignCardForm :beneficiary="beneficiary" :close-modal="closeModal" @cardAssignSuccess="onCardAssignSuccess" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { ref, computed } from "vue";
import { useRoute } from "vue-router";

import AssignCardForm from "@/components/card/assign-card-form.vue";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();

const cardAssignSuccess = ref(false);

const { result } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        firstname
        lastname
        notes
        address
        phone
        email
        postalCode
      }
    }
  `,
  {
    id: route.params.beneficiaryId
  }
);
let beneficiary = useResult(result);

const getBeneficiaryName = computed(() => {
  return beneficiary.value ? `${beneficiary.value.firstname} ${beneficiary.value.lastname}` : "";
});

function onCardAssignSuccess() {
  cardAssignSuccess.value = true;
}
</script>

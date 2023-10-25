<i18n>
{
	"en": {
		"assign-card": "Assign card"
	},
	"fr": {
		"assign-card": "Assigner une carte"
	}
}
</i18n>

<template>
  <!-- root div allows passing props to component -->
  <div>
    <RouterView v-slot="{ Component }">
      <CardAssignationList
        ref="beneficiaryList"
        display-beneficiary-without-subscription
        :selected-organization="props.selectedOrganization"
        :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform">
        <template #actions="{ beneficiary }">
          <UiButtonGroup :items="getBtnGroup(beneficiary)" tooltip-position="left" />
        </template>
        <template #emptyState>
          <slot name="emptyState"></slot>
        </template>
      </CardAssignationList>
      <Component :is="Component" />
    </RouterView>
  </div>
</template>

<script setup>
import { ref, defineProps } from "vue";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import gql from "graphql-tag";
import { useQuery, useResult } from "@vue/apollo-composable";

import { URL_CARDS_ASSIGNATION, URL_CARD_ASSIGN } from "@/lib/consts/urls";
import ICON_CARD_LINK from "@/lib/icons/card-link.json";

import CardAssignationList from "@/components/card/card-assignation-list.vue";

const { t } = useI18n();
const beneficiaryList = ref();

const props = defineProps({
  selectedOrganization: {
    type: String,
    default: ""
  }
});

const { result: resultOrganization } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        id
        project {
          id
          administrationSubscriptionsOffPlatform
        }
      }
    }
  `,
  {
    id: props.selectedOrganization
  }
);

let administrationSubscriptionsOffPlatform = useResult(resultOrganization, null, (data) => {
  return data.organization.project.administrationSubscriptionsOffPlatform;
});

const getBtnGroup = (beneficiary) => [
  {
    label: t("assign-card"),
    icon: ICON_CARD_LINK,
    route: {
      name: URL_CARD_ASSIGN,
      params: { beneficiaryId: beneficiary.id }
    }
  }
];

onBeforeRouteUpdate((to) => {
  if (to.name === URL_CARDS_ASSIGNATION) {
    beneficiaryList.value.refetchBeneficiaries();
  }
});
</script>

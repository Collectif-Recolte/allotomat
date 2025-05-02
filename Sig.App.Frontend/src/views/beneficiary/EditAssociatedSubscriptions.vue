<i18n>
  {
    "en": {
      "add-subscription": "Add a subscription",
      "no-associated-subscription": "No subscription assigned",
      "title": "Subscription(s) - {beneficiaryName}",
      "selected-subscription": "Selected subscription"
    },
    "fr": {
      "add-subscription": "Ajouter un abonnement",
      "no-associated-subscription": "Aucun abonnement associé",
      "title": "Abonnement(s) - {beneficiaryName}",
      "selected-subscription": "Abonnement sélectionné"
    }
  }
</i18n>

<template>
  <UiDialogModal :title="t('title', { beneficiaryName: getBeneficiaryName() })" :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <template v-if="beneficiary">
      <p v-if="beneficiary.beneficiarySubscriptions.length === 0" class="text-sm text-red-500">
        {{ t("no-associated-subscription") }}
      </p>
      <SubscriptionTable v-else :subscriptions="subscriptionItems" />

      <UiSelectAndAdd
        v-if="filteredSubscriptionOptions.length > 0"
        :show-select="addSubscriptionDisplayed"
        :select-label="t('selected-subscription')"
        :add-label="t('add-subscription')"
        :options="filteredSubscriptionOptions"
        @showSelect="showAddSubscription"
        @submit="saveSubscription" />
    </template>
  </UiDialogModal>
</template>

<script setup>
// *****  Currently UNUSED view ***** //
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

import { subscriptionName } from "@/lib/helpers/subscription";

import { useOrganizationStore } from "@/lib/store/organization";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

import SubscriptionTable from "@/components/subscription/subscription-table";

const { t } = useI18n();
const route = useRoute();
const { currentOrganization } = useOrganizationStore();

const addSubscriptionDisplayed = ref(false);

const { result: resultBeneficiary, refetch } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        firstname
        lastname
        beneficiarySubscriptions {
          subscription {
            id
            isArchived
            name
          }
        }
        ... on BeneficiaryGraphType {
          beneficiaryType {
            id
          }
        }
      }
    }
  `,
  {
    id: route.params.beneficiaryId
  }
);

const beneficiary = useResult(resultBeneficiary);

const { result: resultOrganization } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        id
        project {
          subscriptions {
            id
            name
            isArchived
            types {
              id
              beneficiaryType {
                id
              }
            }
          }
        }
      }
    }
  `,
  {
    id: currentOrganization
  },
  () => ({
    enabled: currentOrganization !== null
  })
);

const subscriptionOptions = useResult(resultOrganization, null, (data) => {
  return data.organization.project.subscriptions
    .map((x) => {
      return { label: `${subscriptionName(x)}`, value: `${x.id}`, types: x.types };
    })
    .reduce(function (a, b) {
      return a.concat(b);
    }, []);
});

const { mutate: addBeneficiaryToSubscription } = useMutation(
  gql`
    mutation AddBeneficiaryToSubscription($input: AddBeneficiaryToSubscriptionInput!) {
      addBeneficiaryToSubscription(input: $input) {
        beneficiary {
          id
          beneficiarySubscriptions {
            subscription {
              id
              isArchived
              name
            }
          }
        }
      }
    }
  `
);

const filteredSubscriptionOptions = computed(() => {
  const subscriptions = beneficiary.value.beneficiarySubscriptions.map((x) => x.subscription.id);
  const beneficiaryType = beneficiary.value.beneficiaryType.id;
  if (!subscriptionOptions.value) return [];
  return subscriptionOptions.value.filter(
    (x) => !subscriptions.some((y) => y === x.value) && x.types.some((z) => z.beneficiaryType.id === beneficiaryType)
  );
});

const subscriptionItems = computed(() => {
  const items = beneficiary.value.beneficiarySubscriptions;
  const formattedItems = [];

  for (let item of items) {
    formattedItems.push({
      id: item.subscription.id,
      name: subscriptionName(item.subscription)
    });
  }

  return formattedItems;
});

function getBeneficiaryName() {
  return beneficiary.value ? `${beneficiary.value.firstname} ${beneficiary.value.lastname}` : "";
}

function showAddSubscription() {
  addSubscriptionDisplayed.value = true;
}

async function saveSubscription(subscription) {
  const subscriptionId = subscription.split("|")[0];

  await addBeneficiaryToSubscription({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      subscriptionId
    }
  });

  refetch();

  addSubscriptionDisplayed.value = false;
}
</script>

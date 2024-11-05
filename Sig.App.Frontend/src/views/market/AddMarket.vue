<i18n>
{
	"en": {
		"add-market": "Add",
		"add-market-success-notification": "Adding market {marketName} was successful. Managers will receive an email for the creation of their account in the next few minutes.",
		"title": "Create a market",
		"user-already-manager": "One of the managers is already the manager of a market.",
		"user-not-market-manager": "One of the managers is not a market manager.",
    "manager-email": "Email",
    "market-name": "Market name"
	},
	"fr": {
		"add-market": "Ajouter",
		"add-market-success-notification": "L’ajout du commerce {marketName} a été un succès. Les gestionnaires vont recevoir un courriel pour la création de leur compte dans les prochaines minutes.",
		"title": "Créer un commerce",
		"user-already-manager": "Un des gestionnaires est déjà gestionnaire d'un commerce.",
		"user-not-market-manager": "Un des gestionnaires n'est pas du type gestionnaire de commerce.",
    "manager-email": "Courriel",
    "market-name": "Nom du marché"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="returnRoute()" :title="t('title')" :has-footer="false">
    <MarketForm
      :submit-btn="t('add-market')"
      :initial-values="initialValues"
      :validation-schema="validationSchema"
      :is-in-project="route.name === URL_MARKET_OVERVIEW_ADD"
      :market-group-options="marketGroups"
      is-new
      @closeModal="closeModal"
      @submit="onSubmit">
      <FormSectionManagers />
    </MarketForm>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useMutation, useQuery, useResult } from "@vue/apollo-composable";
import { string, object, array, lazy } from "yup";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_ADMIN, URL_MARKET_OVERVIEW_ADD, URL_MARKET_OVERVIEW } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import FormSectionManagers from "@/components/managers/form-section-managers";
import MarketForm from "@/views/market/_Form.vue";

useGraphQLErrorMessages({
  USER_ALREADY_MANAGER: () => {
    return t("user-already-manager");
  },
  EXISTING_USER_NOT_MERCHANT: () => {
    return t("user-not-market-manager");
  }
});

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result: resultProject } = useQuery(
  gql`
    query Project {
      projects {
        id
        name
        markets {
          id
          name
        }
        marketGroups {
          id
          name
          isArchived
        }
      }
    }
  `,
  null,
  () => ({
    enabled: route.name === URL_MARKET_OVERVIEW_ADD
  })
);
const project = useResult(resultProject, null, (data) => {
  return data.projects[0];
});
const marketGroups = useResult(resultProject, null, (data) => {
  return data.projects[0].marketGroups.map((marketGroup) => ({
    value: marketGroup.id,
    label: marketGroup.name
  }));
});

const { mutate: createMarket } = useMutation(
  gql`
    mutation CreateMarket($input: CreateMarketInput!) {
      createMarket(input: $input) {
        market {
          id
          name
        }
      }
    }
  `
);

const initialValues = {
  managers: [{ email: "" }]
};

const validationSchema = computed(() =>
  object({
    marketName: string().label(t("market-name")).required(),
    managers: array()
      .required()
      .min(1)
      .of(
        object({
          email: string().label(t("manager-email")).required().email()
        })
      ),
    marketGroup: lazy(() => {
      if (route.name !== URL_MARKET_OVERVIEW_ADD) return string().notRequired();
      return string().label(t("selected-market-group")).required();
    })
  })
);

async function onSubmit(values) {
  let input = {
    name: values.marketName,
    managerEmails: values.managers.map((x) => x.email),
    projectId: route.name === URL_MARKET_OVERVIEW_ADD ? { value: project.value.id } : null,
    marketGroupId: route.name === URL_MARKET_OVERVIEW_ADD ? { value: values.marketGroup } : null
  };

  await createMarket({ input });
  router.push(returnRoute());
  addSuccess(t("add-market-success-notification", { ...values.marketName }));
}

function returnRoute() {
  if (route.name === URL_MARKET_OVERVIEW_ADD) return { name: URL_MARKET_OVERVIEW };
  else return { name: URL_MARKET_ADMIN };
}
</script>

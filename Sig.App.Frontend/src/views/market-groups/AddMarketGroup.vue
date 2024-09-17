<i18n>
{
	"en": {
		"add-market-group": "Add",
		"add-market-group-success-notification": "The addition of the market group {marketGroupName} was successful. The managers will receive an email to create their account in the next few minutes.",
		"title": "Add a market group",
		"user-already-manager": "One of the managers is already the manager of a market group.",
		"user-not-market-group-manager": "One of the managers is not a market group manager.",
    "manager-email": "Email",
    "market-group-name": "Market group name"
	},
	"fr": {
		"add-market-group": "Ajouter",
		"add-market-group-success-notification": "L’ajout du groupe de commerce {marketGroupName} a été un succès. Les gestionnaires vont recevoir un courriel pour la création de leur compte dans les prochaines minutes.",
		"title": "Ajouter un groupe de commerce",
		"user-already-manager": "Un des gestionnaires est déjà gestionnaire d'un groupe de commerce.",
		"user-not-market-group-manager": "Un des gestionnaires n'est pas du type gestionnaire de groupe de commerce.",
    "manager-email": "Courriel",
    "market-group-name": "Nom du groupe de commerce"
	}
}
</i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :return-route="{ name: URL_MARKET_GROUPS_OVERVIEW }"
    :title="t('title')"
    :has-footer="false">
    <MarketGroupForm
      :submit-btn="t('add-market-group')"
      :initial-values="initialValues"
      :validation-schema="validationSchema"
      is-new
      @closeModal="closeModal"
      @submit="onSubmit">
      <FormSectionManagers />
    </MarketGroupForm>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";
import { string, object, array } from "yup";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_GROUPS_OVERVIEW } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import FormSectionManagers from "@/components/managers/form-section-managers";
import MarketGroupForm from "@/views/market-groups/_Form.vue";

useGraphQLErrorMessages({
  USER_ALREADY_MANAGER: () => {
    return t("user-already-manager");
  },
  EXISTING_USER_NOT_MERCHANT: () => {
    return t("user-not-market-group-manager");
  }
});

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { mutate: createMarketGroup } = useMutation(
  gql`
    mutation CreateMarketGroup($input: CreateMarketGroupInput!) {
      createMarketGroup(input: $input) {
        marketGroup {
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
    marketGroupName: string().label(t("market-group-name")).required(),
    managers: array()
      .required()
      .min(1)
      .of(
        object({
          email: string().label(t("manager-email")).required().email()
        })
      )
  })
);

async function onSubmit(values) {
  let input = {
    projectId: route.query.projectId,
    name: values.marketGroupName,
    managerEmails: values.managers.map((x) => x.email)
  };

  await createMarketGroup({ input });
  router.push({ name: URL_MARKET_GROUPS_OVERVIEW });
  addSuccess(t("add-market-group-success-notification", { ...values.marketName }));
}
</script>

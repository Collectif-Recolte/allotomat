<i18n>
{
	"en": {
		"add-market": "Add",
		"add-market-success-notification": "Adding market {marketName} was successful. Managers will receive an email for the creation of their account in the next few minutes.",
		"title": "Add a market",
		"user-already-manager": "One of the managers is already the manager of a market.",
		"user-not-market-manager": "One of the managers is not a market manager.",
    "manager-email": "Email",
    "market-name": "Market name"
	},
	"fr": {
		"add-market": "Ajouter",
		"add-market-success-notification": "L’ajout du commerce {marketName} a été un succès. Les gestionnaires vont recevoir un courriel pour la création de leur compte dans les prochaines minutes.",
		"title": "Ajouter un commerce",
		"user-already-manager": "Un des gestionnaires est déjà gestionnaire d'un commerce.",
		"user-not-market-manager": "Un des gestionnaires n'est pas du type gestionnaire de commerce.",
    "manager-email": "Courriel",
    "market-name": "Nom du marché"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_MARKET_ADMIN }" :title="t('title')" :has-footer="false">
    <MarketForm
      :submit-btn="t('add-market')"
      :initial-values="initialValues"
      :validation-schema="validationSchema"
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
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";
import { string, object, array } from "yup";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_ADMIN } from "@/lib/consts/urls";
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
const { addSuccess } = useNotificationsStore();

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
      )
  })
);

async function onSubmit(values) {
  let input = {
    name: values.marketName,
    managerEmails: values.managers.map((x) => x.email)
  };

  await createMarket({ input });
  router.push({ name: URL_MARKET_ADMIN });
  addSuccess(t("add-market-success-notification", { ...values.marketName }));
}
</script>

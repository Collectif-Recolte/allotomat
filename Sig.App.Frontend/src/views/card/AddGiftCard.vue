<i18n>
{
	"en": {
		"assign-card": "Create the gift card",
		"auto-assign-card": "Automatically assign to an ID",
		"cancel": "Cancel",
		"card-not-found": "Card ID does not exist.",
		"existing-card-id": "ID of an existing card",
		"existing-card-id-placeholder": "Ex. 98671",
		"title": "Creation of a gift card",
    "no-available-cards": "No available cards",
    "amount-label": "Amount",
    "amount-placeholder": "Ex. {amount}",
    "warning-create-gift-card": "A gift card is activated as soon as it is created, please do not lose it!",
    "gift-card-fund-sucessfully-added": "The gift card {cardId} is now activated and has {amount}."
	},
	"fr": {
		"assign-card": "Créer la carte-cadeau",
		"auto-assign-card": "Assigner automatiquement à un ID",
		"cancel": "Annuler",
		"card-not-found": "L'ID de la carte n'existe pas.",
		"existing-card-id": "ID d'une carte existante",
		"existing-card-id-placeholder": "Ex. 98671",
		"title": "Création d'une carte-cadeau",
    "no-available-cards": "Aucune carte disponible",
    "amount-label": "Montant",
    "amount-placeholder": "Ex. {amount}",
    "warning-create-gift-card": "Une carte-cadeau est activée dès sa création, veuillez ne pas la perdre!",
    "gift-card-fund-sucessfully-added": "La carte ({cardId}) est maintenant activée en tant que carte-cadeau et possède {amount}."
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_CARDS }" :title="t('title')" :has-footer="false">
    <Form v-if="project" v-slot="{ isSubmitting, setFieldValue }" :validation-schema="validationSchema" @submit="onSubmit">
      <PfForm
        has-footer
        can-cancel
        :submit-label="t('assign-card')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        :warning-message="t('warning-create-gift-card')"
        @cancel="closeModal">
        <PfFormSection>
          <Field v-slot="{ field, errors: fieldErrors }" name="existingCardId">
            <PfFormInputText
              id="existingCardId"
              v-bind="field"
              :label="t('existing-card-id')"
              :placeholder="t('existing-card-id-placeholder')"
              :errors="fieldErrors"
              input-type="number" />
          </Field>
          <PfTooltip
            v-if="project"
            :hide-tooltip="project.cardStats.cardsUnassigned > 0"
            class="group-pfone"
            :label="t('no-available-cards')">
            <PfButtonAction
              class="mb-5"
              type="button"
              :label="t('auto-assign-card')"
              :is-disabled="project.cardStats.cardsUnassigned === 0"
              @click="assignCardAutomatically(setFieldValue)" />
          </PfTooltip>
          <Field v-slot="{ field, errors: fieldErrors }" name="amount">
            <PfFormInputText
              id="amount"
              v-bind="field"
              :label="t('amount-label')"
              :placeholder="t('amount-placeholder', { amount: 18.43 })"
              :errors="fieldErrors"
              input-type="number"
              min="0" />
          </Field>
        </PfFormSection>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { string, number, object, lazy } from "yup";
import { useRoute, useRouter } from "vue-router";

import { URL_CARDS } from "@/lib/consts/urls";

import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";
import { getMoneyFormat } from "@/lib/helpers/money";

import { useNotificationsStore } from "@/lib/store/notifications";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const validationSchema = computed(() =>
  object({
    existingCardId: lazy((value) => {
      if (value === "") return string().label(t("existing-card-id")).required();
      return number().label(t("existing-card-id")).required();
    }),
    amount: number().nullable(true).label(t("amount-label")).required().min(0)
  })
);

useGraphQLErrorMessages({
  CARD_NOT_FOUND: () => {
    return t("card-not-found");
  },
  INVALID_OPERATION: () => {
    return t("card-not-found");
  },
  NULL_REFERENCE: () => {
    return t("card-not-found");
  }
});

const { result: resultProjects } = useQuery(
  gql`
    query Projects {
      projects {
        id
        cardStats {
          cardsUnassigned
        }
      }
    }
  `
);
const project = useResult(resultProjects, null, (data) => {
  return data.projects[0];
});

const { result: resultForecastNextUnassignedCard } = useQuery(
  gql`
    query ForecastNextUnassignedCard($projectId: ID!) {
      forecastNextUnassignedCard(projectId: $projectId)
    }
  `,
  {
    projectId: route.query.projectId
  }
);
const nextUnassignedCard = useResult(resultForecastNextUnassignedCard, null, (data) => {
  return data.forecastNextUnassignedCard;
});

const { mutate: addLoyaltyFundToCard } = useMutation(
  gql`
    mutation AddLoyaltyFundToCard($input: AddLoyaltyFundToCardInput!) {
      addLoyaltyFundToCard(input: $input) {
        transaction {
          id
          amount
        }
      }
    }
  `
);

function assignCardAutomatically(callback) {
  callback("existingCardId", nextUnassignedCard.value);
}

async function onSubmit({ amount, existingCardId }) {
  await addLoyaltyFundToCard({
    input: {
      amount: parseFloat(amount),
      cardId: parseInt(existingCardId),
      projectId: route.query.projectId
    }
  });

  addSuccess(t("gift-card-fund-sucessfully-added", { amount: getMoneyFormat(parseFloat(amount)), cardId: existingCardId }));
  router.push({ name: URL_CARDS });
}
</script>

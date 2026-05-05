<i18n>
{
	"en": {
		"assign-card": "Create the gift card",
		"auto-assign-card": "Use the first unassigned card",
		"cancel": "Cancel",
		"card-not-found": "Card ID does not exist.",
    "card-lost": "The card you are trying to use is lost",
		"existing-card-id": "ID of an existing card",
		"existing-card-id-placeholder": "Ex. 98671",
		"title": "Create a Gift Card",
    "no-available-cards": "No available cards",
    "amount-label": "Amount",
    "amount-placeholder": "Ex. {amount}",
    "warning-create-gift-card": "A gift card is activated as soon as it is created. Please do not lose it!",
    "gift-card-fund-successfully-added": "The gift card {cardId} is now activated and has {amount}.",
    "warning-create-gift-card-already-used": "This card is already in use (status: {cardStatus}). It contains <b>{subscriptionAmount}</b> in subscription funds and <b>{giftCardAmount}</b> in gift funds. Before proceeding, please make sure you would like to add gift funds onto this card.",
    "card-status-assigned": "assigned to a participant",
    "card-status-gift-card": "gift card",
    "card-status-unassigned": "unassigned card",
    "card-status-lost": "lost card",
    "card-status-deactivated": "deactivated card"
	},
	"fr": {
		"assign-card": "Créer la carte-cadeau",
		"auto-assign-card": "Utiliser la première carte non assignée",
		"cancel": "Annuler",
		"card-not-found": "L'ID de la carte n'existe pas.",
    "card-lost": "La carte que vous tentez d'utiliser est perdue.",
		"existing-card-id": "ID d'une carte existante",
		"existing-card-id-placeholder": "Ex. 98671",
		"title": "Création d'une carte-cadeau",
    "no-available-cards": "Aucune carte disponible",
    "amount-label": "Montant",
    "amount-placeholder": "Ex. {amount}",
    "warning-create-gift-card": "Une carte-cadeau est activée dès sa création, veuillez ne pas la perdre!",
    "gift-card-fund-successfully-added": "La carte ({cardId}) est maintenant activée en tant que carte-cadeau et possède {amount}.",
    "warning-create-gift-card-already-used": "Cette carte est déjà utilisée (statut : {cardStatus}) : elle contient <b>{subscriptionAmount}</b> de fonds d’abonnement et <b>{giftCardAmount}</b> de fonds cadeaux. Assurez-vous qu'il s'agit de la bonne carte pour l'ajout de fonds cadeaux.",
    "card-status-assigned": "attribuée à un·e participant·e",
    "card-status-gift-card": "carte-cadeau",
    "card-status-unassigned": "carte non attribuée",
    "card-status-lost": "carte perdue",
    "card-status-deactivated": "carte désactivée"
	}
}
</i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :return-route="{ name: URL_CARDS_MANAGE_GIFT_CARDS }"
    :title="t('title')"
    :has-footer="false">
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
          <Field v-slot="{ field, errors: fieldErrors }" name="existingCardId" class="grid grid-cols-1 gap-y-2">
            <div class="flex flex-col gap-y-2">
              <PfFormInputText
                id="existingCardId"
                v-bind="field"
                :label="t('existing-card-id')"
                :placeholder="t('existing-card-id-placeholder')"
                :errors="fieldErrors"
                input-type="number"
                @input="(e) => updateCardIdToQuery(e)" />
              <UiCallout
                v-if="cardById && showAlreadyUsedWarning"
                :variant="CALLOUT_WARNING"
                :message="
                  t('warning-create-gift-card-already-used', {
                    cardStatus: cardStatusLabel,
                    subscriptionAmount: getMoneyFormat(cardById.totalFund),
                    giftCardAmount: getMoneyFormat(cardById.loyaltyFund?.amount ?? 0)
                  })
                "
                allow-html />
            </div>
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
import { ref, computed } from "vue";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { string, number, object, lazy } from "yup";
import { useRoute, useRouter } from "vue-router";

import { URL_CARDS_MANAGE_GIFT_CARDS } from "@/lib/consts/urls";
import { CARD_STATUS_UNASSIGNED } from "@/lib/consts/enums";
import { CALLOUT_WARNING } from "@/lib/consts/callout";

import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";
import { getMoneyFormat } from "@/lib/helpers/money";

import UiCallout from "@/components/ui/callout.vue";

import { useNotificationsStore } from "@/lib/store/notifications";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const showAlreadyUsedWarning = computed(() => {
  const card = cardById.value;
  return card && card.status !== CARD_STATUS_UNASSIGNED;
});

const cardStatusLabel = computed(() => {
  const status = cardById.value?.status;
  if (!status) return "";
  const key = "card-status-" + String(status).toLowerCase().replace(/_/g, "-");
  const label = t(key);
  return label !== key ? label : String(status);
});

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
  },
  CARD_LOST: () => {
    return t("card-lost");
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

const existingCardIdToQuery = ref(0);

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

const cardIdToQuery = computed(() => {
  return existingCardIdToQuery.value;
});

const { result: resultCardById } = useQuery(
  gql`
    query CardByProgramCardId($projectId: ID!, $programCardId: String!) {
      project(id: $projectId) {
        id
        cardByProgramCardId(programCardId: $programCardId) {
          id
          status
          totalFund
          loyaltyFund {
            id
            amount
          }
        }
      }
    }
  `,
  {
    projectId: route.query.projectId,
    programCardId: cardIdToQuery
  },
  () => ({
    enabled: cardIdToQuery.value !== null
  })
);
const cardById = useResult(resultCardById, null, (data) => {
  return data.project.cardByProgramCardId;
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
  updateCardIdToQuery(nextUnassignedCard.value);
}

function updateCardIdToQuery(val) {
  existingCardIdToQuery.value = val;
}

async function onSubmit({ amount, existingCardId }) {
  await addLoyaltyFundToCard({
    input: {
      amount: parseFloat(amount),
      cardId: parseInt(existingCardId),
      projectId: route.query.projectId
    }
  });

  addSuccess(t("gift-card-fund-successfully-added", { amount: getMoneyFormat(parseFloat(amount)), cardId: existingCardId }));
  router.push({ name: URL_CARDS_MANAGE_GIFT_CARDS });
}
</script>

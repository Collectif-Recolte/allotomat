<i18n>
{
	"en": {
		"edit-funds-gift-card": "Adjust balance",
		"cancel": "Cancel",
		"title": "Adjust Gift Card Balance",
		"funds-label": "Gift card funds",
		"funds-placeholder": "Ex. 100",
		"gift-card-fund-successfully-edited": "Gift card balance successfully udpated.",
		"card-not-found": "Card ID does not exist.",
		"card-lost": "The card you are trying to use is lost."
	},
	"fr": {
		"edit-funds-gift-card": "Modifier le solde",
		"cancel": "Annuler",
		"title": "Modifier le solde d'une carte-cadeau",
		"funds-label": "Fonds carte-cadeau",
		"funds-placeholder": "Ex. 100",
		"gift-card-fund-successfully-edited": "Le solde de la carte-cadeau a été modifié avec succès.",
		"card-not-found": "L'ID de la carte n'existe pas.",
		"card-lost": "La carte que vous tentez d'utiliser est perdue."
	},
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="returnRoute" :title="t('title')" :has-footer="false">
    <Form
      v-if="card"
      v-slot="{ isSubmitting }"
      :validation-schema="validationSchema"
      :initial-values="initialValues"
      @submit="onSubmit">
      <PfForm
        has-footer
        can-cancel
        :submit-label="t('edit-funds-gift-card')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        @cancel="closeModal">
        <PfFormSection>
          <Field v-slot="{ field, errors: fieldErrors }" name="amount">
            <PfFormInputText
              id="amount"
              v-bind="field"
              :label="t('funds-label')"
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
import { useRouter, useRoute } from "vue-router";

import { URL_CARDS_MANAGE_GIFT_CARDS, URL_BENEFICIARY_ADMIN, URL_BENEFICIARY_EDIT_GIFT_CARD } from "@/lib/consts/urls";

import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import { useNotificationsStore } from "@/lib/store/notifications";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const returnRoute = computed(() => {
  if (route.name === URL_BENEFICIARY_EDIT_GIFT_CARD) {
    return { name: URL_BENEFICIARY_ADMIN };
  } else {
    return { name: URL_CARDS_MANAGE_GIFT_CARDS };
  }
});

const validationSchema = computed(() =>
  object({
    amount: lazy((value) => {
      if (value === "") return string().label(t("funds-label")).required();
      return number().label(t("funds-label")).required().min(0);
    })
  })
);

useGraphQLErrorMessages({
  CARD_NOT_FOUND: () => t("card-not-found"),
  CARD_IS_NOT_GIFT_CARD: () => t("card-not-found"),
  INVALID_OPERATION: () => t("card-not-found"),
  NULL_REFERENCE: () => t("card-not-found"),
  CARD_LOST: () => t("card-lost")
});

const { mutate: editLoyaltyFundOnCard } = useMutation(
  gql`
    mutation EditLoyaltyFundOnCard($input: EditLoyaltyFundOnCardInput!) {
      editLoyaltyFundOnCard(input: $input) {
        transaction {
          id
          amount
        }
      }
    }
  `
);

const { result: resultCard } = useQuery(
  gql`
    query Card($id: ID!) {
      card(id: $id) {
        id
        loyaltyFund {
          id
          amount
        }
      }
    }
  `,
  {
    id: route.params.cardId
  }
);

const card = useResult(resultCard, null, (data) => data?.card ?? null);

const initialValues = computed(() => ({
  amount: card.value?.loyaltyFund != null ? parseFloat(card.value.loyaltyFund.amount) : 0
}));

async function onSubmit({ amount }) {
  const cardId = card.value?.id ?? route.params.cardId;
  if (cardId == null) {
    return;
  }

  await editLoyaltyFundOnCard({
    input: {
      amount: parseFloat(amount),
      cardId: route.params.cardId
    }
  });

  addSuccess(t("gift-card-fund-successfully-edited"));
  router.push(returnRoute.value);
}
</script>

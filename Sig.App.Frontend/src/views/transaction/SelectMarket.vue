<i18n>
  {
    "en": {
      "sub-title": "Purchase on behalf of program",
      "select-market": "Merchant",
      "choose-market": "Select",
      "next-step": "Next",
      "cancel": "Cancel",
      "transaction-in-organization-name": "Purchase on behalf of organization"
    },
    "fr": {
      "sub-title": "Achat au nom du programme",
      "select-market": "Marchand",
      "choose-market": "Sélectionner",
      "next-step": "Suivant",
      "cancel": "Annuler",
      "transaction-in-organization-name": "Achat au nom d'une organisation"
    }
  }
  </i18n>

<template>
  <p class="text-p1">
    {{ t("transaction-in-organization-name") }}
  </p>
  <Form v-slot="{ errors: formErrors }" :validation-schema="validationSchema" keep-values @submit="nextStep">
    <PfForm
      has-footer
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="t('next-step')"
      :cancel-label="t('cancel')"
      footer-alt-style
      can-cancel
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field: inputField, errors: fieldErrors }" name="marketId">
          <PfFormInputSelect
            id="marketId"
            v-bind="inputField"
            :placeholder="t('choose-market')"
            :label="t('select-market')"
            :options="markets"
            :errors="fieldErrors" />
        </Field>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useQuery, useResult } from "@vue/apollo-composable";

import { TRANSACTION_STEPS_ADD } from "@/lib/consts/enums";

const { t } = useI18n();

const emit = defineEmits(["onUpdateStep", "onCloseModal"]);

const validationSchema = computed(() =>
  object({
    marketId: string().label(t("select-market")).required()
  })
);

const { result: resultOrganizations } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        markets {
          id
          name
        }
      }
    }
  `
);
const markets = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].markets.map((x) => {
    return {
      label: x.name,
      value: x.id
    };
  });
});

async function nextStep(values) {
  emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
    marketId: values.marketId
  });
}

function closeModal() {
  emit("onCloseModal");
}
</script>

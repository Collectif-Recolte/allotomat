<i18n>
  {
    "en": {
      "sub-title": "Purchase on behalf of program",
      "select-market": "Merchant",
      "choose-market": "Select",
      "next-step": "Next",
      "cancel": "Cancel",
      "transaction-in-project-name": "Purchase on behalf of project",
      "transaction-in-organization-name": "Purchase on behalf of organization"
    },
    "fr": {
      "sub-title": "Achat au nom du programme",
      "select-market": "Marchand",
      "choose-market": "Sélectionner",
      "next-step": "Suivant",
      "cancel": "Annuler",
      "transaction-in-project-name": "Achat au nom d'un programme",
      "transaction-in-organization-name": "Achat au nom d'une organisation"
    }
  }
  </i18n>

<template>
  <div v-if="markets != null && markets.length > 0">
    <p class="text-p1">
      {{ userType === USER_TYPE_ORGANIZATIONMANAGER ? t("transaction-in-organization-name") : t("transaction-in-project-name") }}
    </p>
    <Form
      v-slot="{ errors: formErrors }"
      :validation-schema="validationSchema"
      :initial-values="initialValues"
      keep-values
      @submit="nextStep">
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
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineProps, defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useQuery, useResult } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";

import { useAuthStore } from "@/lib/store/auth";

import { TRANSACTION_STEPS_ADD, USER_TYPE_ORGANIZATIONMANAGER } from "@/lib/consts/enums";

const { t } = useI18n();
const { userType } = storeToRefs(useAuthStore());

const emit = defineEmits(["onUpdateStep", "onCloseModal"]);

const props = defineProps({
  marketId: {
    type: String,
    default: ""
  }
});

const initialValues = {
  marketId: props.marketId
};

const validationSchema = computed(() =>
  object({
    marketId: string().label(t("select-market")).required()
  })
);

const { result: resultAllMarkets } = useQuery(
  gql`
    query Markets {
      allMarkets {
        id
        name
      }
    }
  `
);
const allMarkets = useResult(resultAllMarkets, null, (data) => {
  return data.allMarkets.map((market) => ({
    value: market.id,
    label: market.name
  }));
});

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
const organizationMarkets = useResult(resultOrganizations, null, (data) => {
  if (data.organizations[0].markets.length === 1) {
    emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
      marketId: data.organizations[0].markets[0].id
    });
    return [];
  }
  return data.organizations[0].markets.map((x) => {
    return {
      label: x.name,
      value: x.id
    };
  });
});

const markets = computed(() => {
  if (userType.value === USER_TYPE_ORGANIZATIONMANAGER) {
    return organizationMarkets.value;
  }
  return allMarkets.value;
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

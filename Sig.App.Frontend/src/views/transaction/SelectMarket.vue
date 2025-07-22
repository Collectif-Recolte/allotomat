<i18n>
  {
    "en": {
      "select-market": "Merchant",
      "choose-market": "Select",
      "next-step": "Next",
      "cancel": "Cancel",
      "transaction-in-project-name": "Purchase on behalf of project",
      "transaction-in-organization-name": "Purchase on behalf of organization",
      "select-cash-register": "Cash Register",
      "choose-cash-register": "Select",
      "market-disabled-label": "{market} is disabled"
    },
    "fr": {
      "select-market": "Marchand",
      "choose-market": "Sélectionner",
      "next-step": "Suivant",
      "cancel": "Annuler",
      "transaction-in-project-name": "Achat au nom d'un programme",
      "transaction-in-organization-name": "Achat au nom d'une organisation",
      "select-cash-register": "Caisse",
      "choose-cash-register": "Sélectionner",
      "market-disabled-label": "{market} est désactivé"
    }
  }
  </i18n>

<template>
  <div>
    <p class="text-p1">
      {{ userType === USER_TYPE_ORGANIZATIONMANAGER ? t("transaction-in-organization-name") : t("transaction-in-project-name") }}
    </p>
    <Form
      v-slot="{ errors: formErrors }"
      :validation-schema="validationSchema"
      :initial-values="initialValues"
      :initial-touched="initialTouched"
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
              :errors="fieldErrors"
              @input="onMarketSelected" />
          </Field>
          <Field v-slot="{ field: inputField, errors: fieldErrors }" name="cashRegisterId">
            <PfFormInputSelect
              id="cashRegisterId"
              v-bind="inputField"
              :disabled="!selectedMarket"
              :placeholder="t('choose-cash-register')"
              :label="t('select-cash-register')"
              :options="cashRegisters"
              :errors="fieldErrors" />
          </Field>
        </PfFormSection>
      </PfForm>
    </Form>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineProps, defineEmits, ref } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useQuery, useResult } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";

import { useAuthStore } from "@/lib/store/auth";

import { TRANSACTION_STEPS_ADD, USER_TYPE_ORGANIZATIONMANAGER } from "@/lib/consts/enums";

const { t } = useI18n();
const { userType } = storeToRefs(useAuthStore());

const emit = defineEmits(["onUpdateStep", "onCloseModal"]);

const selectedMarket = ref("");

const props = defineProps({
  marketId: {
    type: String,
    default: ""
  },
  cashRegisterId: {
    type: String,
    default: ""
  }
});

const initialValues = computed(() => {
  return { marketId: props.marketId, cashRegisterId: props.cashRegisterId };
});

const initialTouched = computed(() => {
  return { marketId: props.marketId !== "", cashRegisterId: props.cashRegisterId !== "" };
});

const validationSchema = computed(() =>
  object({
    marketId: string().label(t("select-market")).required(),
    cashRegisterId: string().label(t("select-cash-register")).required()
  })
);

const { result: resultProjects } = useQuery(
  gql`
    query Projects {
      projects {
        id
        markets {
          id
          name
          isDisabled
          cashRegisters(includeArchived: false) {
            id
            name
          }
        }
      }
    }
  `
);
const projectMarkets = useResult(resultProjects, null, (data) => {
  if (
    data.projects[0].markets.filter((x) => !x.isDisabled).length === 1 &&
    data.projects[0].markets.filter((x) => !x.isDisabled)[0].cashRegisters.length === 1
  ) {
    emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
      marketId: data.projects[0].markets[0].id,
      cashRegisterId: data.projects[0].markets[0].cashRegisters[0].id
    });
    return [];
  }
  return data.projects[0].markets
    .map((x) => {
      return {
        label: x.isDisabled ? t("market-disabled-label", { market: x.name }) : x.name,
        value: x.id,
        isDisabled: x.isDisabled
      };
    })
    .sort((a, b) => a.label.localeCompare(b.label));
});
const projectCashRegisters = useResult(resultProjects, null, (data) => {
  return data.projects[0].markets
    .find((x) => x.id === selectedMarket.value)
    .cashRegisters.map((x) => {
      return {
        label: x.name,
        value: x.id
      };
    })
    .sort((a, b) => a.label.localeCompare(b.label));
});

const { result: resultOrganizations } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        markets {
          id
          name
          cashRegisters(includeArchived: false) {
            id
            name
          }
        }
      }
    }
  `
);
const organizationMarkets = useResult(resultOrganizations, null, (data) => {
  if (data.organizations[0].markets.length === 1 && data.organizations[0].markets[0].cashRegisters.length === 1) {
    emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
      marketId: data.organizations[0].markets[0].id,
      cashRegisterId: data.organizations[0].markets[0].cashRegisters[0].id
    });
    return [];
  }
  return data.organizations[0].markets
    .map((x) => {
      return {
        label: x.name,
        value: x.id
      };
    })
    .sort((a, b) => a.label.localeCompare(b.label));
});
const organizationCashRegisters = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].markets
    .find((x) => x.id === selectedMarket.value)
    .cashRegisters.map((x) => {
      return {
        label: x.name,
        value: x.id
      };
    })
    .sort((a, b) => a.label.localeCompare(b.label));
});

const markets = computed(() => {
  if (userType.value === USER_TYPE_ORGANIZATIONMANAGER) {
    return organizationMarkets.value;
  }
  return projectMarkets.value;
});

const cashRegisters = computed(() => {
  if (userType.value === USER_TYPE_ORGANIZATIONMANAGER) {
    return organizationCashRegisters.value;
  }
  return projectCashRegisters.value;
});

function onMarketSelected(e) {
  selectedMarket.value = e;
}

async function nextStep(values) {
  emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
    marketId: values.marketId,
    cashRegisterId: values.cashRegisterId
  });
}

function closeModal() {
  emit("onCloseModal");
}
</script>

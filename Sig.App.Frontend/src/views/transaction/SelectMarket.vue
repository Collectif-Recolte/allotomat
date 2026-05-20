<i18n>
  {
    "en": {
      "select-market": "Merchant",
      "choose-market": "Select",
      "next-step": "Next",
      "cancel": "Cancel",
      "transaction-in-project-name": "Purchase on behalf of a merchant",
      "transaction-in-organization-name": "Purchase on behalf of an organization",
      "transaction-in-market-group-name": "Purchase on behalf of a market group",
      "select-cash-register": "Cash Register",
      "choose-cash-register": "Select",
      "market-disabled-label": "{market} is deactivated"
    },
    "fr": {
      "select-market": "Commerce",
      "choose-market": "Sélectionner",
      "next-step": "Suivant",
      "cancel": "Annuler",
      "transaction-in-project-name": "Achat au nom d'un commerce",
      "transaction-in-organization-name": "Achat au nom d'un groupe de participant·e·s",
      "transaction-in-market-group-name": "Achat au nom d'un groupe de marchés",
      "select-cash-register": "Caisse",
      "choose-cash-register": "Sélectionner",
      "market-disabled-label": "{market} est désactivé"
    }
  }
</i18n>

<template>
  <div>
    <p class="text-p1">
      {{ descriptionText }}
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
          <Field
            v-if="userType !== USER_TYPE_MARKETGROUPMANAGER"
            v-slot="{ field: inputField, errors: fieldErrors }"
            name="marketId">
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
              :disabled="userType !== USER_TYPE_MARKETGROUPMANAGER && !selectedMarket"
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
import { computed, defineProps, defineEmits, ref, onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useQuery, useResult } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";

import { useAuthStore } from "@/lib/store/auth";

import { TRANSACTION_STEPS_ADD, USER_TYPE_ORGANIZATIONMANAGER, USER_TYPE_MARKETGROUPMANAGER } from "@/lib/consts/enums";

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

onMounted(() => {
  selectedMarket.value = props.marketId;
});

const initialValues = computed(() => {
  return { marketId: props.marketId, cashRegisterId: props.cashRegisterId };
});

const initialTouched = computed(() => {
  return { marketId: props.marketId !== "", cashRegisterId: props.cashRegisterId !== "" };
});

const validationSchema = computed(() => {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) {
    return object({
      cashRegisterId: string().label(t("select-cash-register")).required()
    });
  }
  return object({
    marketId: string().label(t("select-market")).required(),
    cashRegisterId: string().label(t("select-cash-register")).required()
  });
});

const descriptionText = computed(() => {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) return t("transaction-in-market-group-name");
  if (userType.value === USER_TYPE_ORGANIZATIONMANAGER) return t("transaction-in-organization-name");
  return t("transaction-in-project-name");
});

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
  const enabledMarkets = data.projects[0].markets.filter((x) => !x.isDisabled);

  if (!enabledMarkets.find((x) => x.id === selectedMarket.value)) {
    selectedMarket.value = "";
  }

  if (enabledMarkets.length === 1 && enabledMarkets[0].cashRegisters.length === 1) {
    emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
      marketId: enabledMarkets[0].id,
      cashRegisterId: enabledMarkets[0].cashRegisters[0].id
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
const organizationMarkets = useResult(resultOrganizations, null, (data) => {
  const enabledMarkets = data.organizations[0].markets.filter((x) => !x.isDisabled);
  if (enabledMarkets.length === 1 && enabledMarkets[0].cashRegisters.length === 1) {
    emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
      marketId: enabledMarkets[0].id,
      cashRegisterId: enabledMarkets[0].cashRegisters[0].id
    });
    return [];
  }
  return data.organizations[0].markets
    .map((x) => {
      return {
        label: x.isDisabled ? t("market-disabled-label", { market: x.name }) : x.name,
        value: x.id,
        isDisabled: x.isDisabled
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

const { result: resultMarketGroups } = useQuery(
  gql`
    query MarketGroups {
      marketGroups {
        id
        cashRegisters {
          id
          name
          market {
            id
          }
        }
      }
    }
  `,
  null,
  { enabled: computed(() => userType.value === USER_TYPE_MARKETGROUPMANAGER) }
);
const marketGroupCashRegisters = useResult(resultMarketGroups, null, (data) => {
  const allCashRegisters = data.marketGroups.flatMap((mg) => mg.cashRegisters);
  return allCashRegisters.map((cr) => ({ label: cr.name, value: cr.id })).sort((a, b) => a.label.localeCompare(b.label));
});

const stopSingleCashRegisterWatch = watch(marketGroupCashRegisters, (list) => {
  if (list && list.length === 1 && resultMarketGroups.value) {
    const allCashRegisters = resultMarketGroups.value.marketGroups.flatMap((mg) => mg.cashRegisters);
    emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
      marketId: allCashRegisters[0].market.id,
      cashRegisterId: allCashRegisters[0].id
    });
    stopSingleCashRegisterWatch();
  }
});
const cashRegisterMarketMap = useResult(resultMarketGroups, {}, (data) => {
  return Object.fromEntries(data.marketGroups.flatMap((mg) => mg.cashRegisters.map((cr) => [cr.id, cr.market.id])));
});

const markets = computed(() => {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) return [];
  if (userType.value === USER_TYPE_ORGANIZATIONMANAGER) return organizationMarkets.value;
  return projectMarkets.value;
});

const cashRegisters = computed(() => {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) return marketGroupCashRegisters.value;
  if (userType.value === USER_TYPE_ORGANIZATIONMANAGER) return organizationCashRegisters.value;
  return projectCashRegisters.value;
});

function onMarketSelected(e) {
  selectedMarket.value = e;
}

async function nextStep(values) {
  const marketId =
    userType.value === USER_TYPE_MARKETGROUPMANAGER ? cashRegisterMarketMap.value[values.cashRegisterId] : values.marketId;

  emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
    marketId,
    cashRegisterId: values.cashRegisterId
  });
}

function closeModal() {
  emit("onCloseModal");
}
</script>

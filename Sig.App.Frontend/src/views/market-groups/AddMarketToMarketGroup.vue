<i18n>
  {
    "en": {
      "add-market": "Add",
      "add-market-success-notification": "Successfully added market {marketName} to the market group.",
      "title": "Add a market to the market group",
      "choose-market": "Select",
      "select-market": "Market",
      "cancel": "Cancel",
      "no-associated-merchant": "All available markets are associated with the market group."
    },
    "fr": {
      "add-market": "Ajouter",
      "add-market-success-notification": "L’ajout du commerce {marketName} au groupe de commerce a été un succès.",
      "title": "Ajouter un commerce au groupe de commerce",
      "choose-market": "Sélectionner",
      "select-market": "Commerce",
      "cancel": "Annuler",
      "no-associated-merchant": "Tous les commerces disponibles sont associés au groupe de commerce."
    }
  }
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="returnRoute()" :title="t('title')" :has-footer="false">
    <Form
      v-if="!loadingMarketGroup"
      v-slot="{ isSubmitting, meta }"
      :validation-schema="validationSchema || baseValidationSchema"
      :initial-values="initialValues"
      @submit="onSubmit">
      <PfForm
        has-footer
        can-cancel
        :disable-submit="!meta.valid"
        :submit-label="t('add-market')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        @cancel="closeModal">
        <div>
          <div class="flex flex-col gap-y-6">
            <PfFormSection v-if="filteredMarketOptions.length > 0">
              <Field v-slot="{ field: inputField, errors: fieldErrors }" name="market">
                <PfFormInputSelect
                  id="marketId"
                  required
                  :model-value="inputField.value"
                  :placeholder="t('choose-market')"
                  :label="t('select-market')"
                  :options="filteredMarketOptions"
                  :errors="fieldErrors"
                  @update:modelValue="inputField.onChange" />
              </Field>
            </PfFormSection>
            <template v-else>
              <div class="text-red-500">
                <p class="text-sm">{{ t("no-associated-merchant") }}</p>
              </div>
            </template>
          </div>
        </div>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useMutation, useQuery, useResult } from "@vue/apollo-composable";
import { string, object } from "yup";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_GROUP_MANAGE_MERCHANTS } from "@/lib/consts/urls";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result: resultMarketGroup, loading: loadingMarketGroup } = useQuery(
  gql`
    query MarketGroup($id: ID!) {
      marketGroup(id: $id) {
        id
        name
        markets {
          id
          name
        }
        project {
          id
          markets {
            id
            name
          }
        }
      }
    }
  `,
  {
    id: route.params.marketGroupId
  }
);
const marketGroup = useResult(resultMarketGroup, null, (data) => {
  return data.marketGroup;
});
const markets = useResult(resultMarketGroup, null, (data) => {
  return data.marketGroup.project.markets
    .map((market) => ({
      value: market.id,
      label: market.name
    }))
    .sort((a, b) => a.label.localeCompare(b.label));
});

const { mutate: addMarketToMarketGroup } = useMutation(
  gql`
    mutation AddMarketToMarketGroup($input: AddMarketToMarketGroupInput!) {
      addMarketToMarketGroup(input: $input) {
        marketGroup {
          id
          name
          markets {
            id
            name
          }
        }
      }
    }
  `
);

const initialValues = {
  market: null
};

const filteredMarketOptions = computed(() => {
  if (!markets.value || !marketGroup.value) return [];
  return markets.value.filter((x) => !marketGroup.value.markets.some((y) => y.id === x.value));
});

const validationSchema = computed(() =>
  object({
    market: string().label(t("select-market")).required()
  })
);

async function onSubmit(values) {
  await addMarketToMarketGroup({
    input: {
      marketId: values.market,
      marketGroupId: marketGroup.value.id
    }
  });
  router.push(returnRoute());
  addSuccess(t("add-market-success-notification", { ...values.market }));
}

function returnRoute() {
  return { name: URL_MARKET_GROUP_MANAGE_MERCHANTS };
}
</script>

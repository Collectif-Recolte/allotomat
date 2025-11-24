<i18n>
{
	"en": {
		"add-market": "Add",
		"add-market-success-notification": "Successfully added market {marketName} to the program.",
		"title": "Add a market to the program",
    "create-market": "Create Market",
    "choose-market": "Select",
    "select-market": "Market",
    "cancel": "Cancel",
    "no-associated-merchant": "All available markets are associated with the program.",
    "selected-market-group": "Market group"
	},
	"fr": {
		"add-market": "Ajouter",
		"add-market-success-notification": "L’ajout du commerce {marketName} au programme a été un succès.",
		"title": "Ajouter un commerce au programme",
    "create-market": "Créer un commerce",
    "choose-market": "Sélectionner",
    "select-market": "Commerce",
    "cancel": "Annuler",
    "no-associated-merchant": "Tous les commerces disponibles sont associés au programme.",
    "selected-market-group": "Groupe de commerce"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="returnRoute()" :title="t('title')" :has-footer="false">
    <Form
      v-if="!loadingMarkets && !loadingProject"
      v-slot="{ isSubmitting, errors: formErrors }"
      :validation-schema="validationSchema || baseValidationSchema"
      :initial-values="initialValues"
      @submit="onSubmit">
      <PfForm
        has-footer
        can-cancel
        :disable-submit="Object.keys(formErrors).length > 0"
        :submit-label="t('add-market')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        @cancel="closeModal">
        <div>
          <div class="flex flex-col gap-y-6">
            <PfFormSection v-if="filteredMarketOptions.length > 0">
              <Field v-slot="{ field: inputField, errors: fieldErrors }" name="market">
                <UiCombobox
                  id="marketId"
                  required
                  v-bind="inputField"
                  :label="t('select-market')"
                  :options="filteredMarketOptions"
                  :errors="fieldErrors" />
              </Field>
              <Field v-slot="{ field, errors: fieldErrors }" name="marketGroup">
                <PfFormInputSelect
                  id="marketGroup"
                  required
                  v-bind="field"
                  :label="t('selected-market-group')"
                  :options="marketGroups"
                  :disabled="selectMarketGroupEnabled"
                  :errors="fieldErrors" />
              </Field>
            </PfFormSection>
            <template v-else>
              <div class="text-red-500">
                <p class="text-sm">{{ t("no-associated-merchant") }}</p>
              </div>
            </template>
            <PfButtonAction
              v-if="!selectMarketGroupEnabled && canCreateMarket"
              btn-style="dash"
              has-icon-left
              type="button"
              :label="t('create-market')"
              @click="createMarket" />
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
import {
  URL_MARKET_ADMIN,
  URL_MARKET_OVERVIEW_SELECT,
  URL_MARKET_OVERVIEW,
  URL_MARKET_OVERVIEW_ADD,
  URL_ADD_MERCHANTS_FROM_MARKET_GROUP,
  URL_MARKET_GROUP_MANAGE_MERCHANTS,
  URL_ADD_MERCHANTS_FROM_PROJECT,
  URL_PROJECT_MANAGE_MERCHANTS
} from "@/lib/consts/urls";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result, loading: loadingMarkets } = useQuery(
  gql`
    query Markets {
      allMarkets {
        id
        name
      }
    }
  `
);
const markets = useResult(result, null, (data) => {
  return data.allMarkets.map((market) => ({
    value: market.id,
    label: market.name
  }));
});

const { result: resultProject, loading: loadingProject } = useQuery(
  gql`
    query Project {
      projects {
        id
        name
        markets {
          id
          name
        }
        marketGroups {
          id
          name
          isArchived
        }
      }
    }
  `
);
const project = useResult(resultProject, null, (data) => {
  if (route.params.projectId) {
    return data.projects.find((project) => project.id === route.params.projectId);
  }
  return data.projects[0];
});

const marketGroups = useResult(resultProject, null, (data) => {
  var project = data.projects[0];
  if (route.params.projectId) {
    project = data.projects.find((project) => project.id === route.params.projectId);
  }

  return project.marketGroups.map((marketGroup) => ({
    value: marketGroup.id,
    label: marketGroup.name
  }));
});

const { mutate: addMarketToProject } = useMutation(
  gql`
    mutation AddMarketToProject($input: AddMarketToProjectInput!) {
      addMarketToProject(input: $input) {
        project {
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
  market: null,
  marketGroup: route.params.marketGroupId !== null ? route.params.marketGroupId : null
};

const filteredMarketOptions = computed(() => {
  if (!markets.value || !project.value) return [];
  return markets.value.filter((x) => !project.value.markets.some((y) => y.id === x.value));
});

const validationSchema = computed(() =>
  object({
    market: string().label(t("select-market")).required(),
    marketGroup: string().label(t("selected-market-group")).required()
  })
);

const selectMarketGroupEnabled = computed(() => route.name === URL_ADD_MERCHANTS_FROM_MARKET_GROUP);
const canCreateMarket = computed(() => route.name !== URL_ADD_MERCHANTS_FROM_PROJECT);

function createMarket() {
  router.push({ name: URL_MARKET_OVERVIEW_ADD });
}

async function onSubmit(values) {
  await addMarketToProject({
    input: {
      projectId: project.value.id,
      marketId: values.market,
      marketGroupId: values.marketGroup
    }
  });
  router.push(returnRoute());
  addSuccess(t("add-market-success-notification", { ...values.market }));
}

function returnRoute() {
  if (route.name === URL_ADD_MERCHANTS_FROM_MARKET_GROUP) return { name: URL_MARKET_GROUP_MANAGE_MERCHANTS };
  if (route.name === URL_ADD_MERCHANTS_FROM_PROJECT) return { name: URL_PROJECT_MANAGE_MERCHANTS };
  if (route.name === URL_MARKET_OVERVIEW_SELECT) return { name: URL_MARKET_OVERVIEW };
  else return { name: URL_MARKET_ADMIN };
}
</script>

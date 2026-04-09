<i18n>
{
	"en": {
		"add-market": "Add",
		"add-market-success-notification": "The merchant {marketName} has been successfully added to the program.",
		"title": "Add a merchant to the program",
    "create-market": "Create Merchant",
    "choose-market": "Search for a merchant...",
    "select-market": "Search Existing Tomat Merchants",
    "cancel": "Cancel",
    "no-associated-merchant": "All available merchants are associated with the program.",
    "selected-market-group": "Merchant Group",
    "no-results-found": "No merchants found",
    "warning-message":"Note: This list contains merchants from other programs on Tomat. Adding one of these merchants to your program will allow them to accept your cards, and will also create a new cash register that will need to be selected when they open Tomat. Please proceed with caution."
	},
	"fr": {
		"add-market": "Ajouter",
		"add-market-success-notification": "L’ajout du commerce {marketName} au programme a été un succès.",
		"title": "Ajouter un commerce au programme",
    "create-market": "Créer un commerce",
    "choose-market": "Chercher un commerce...",
    "select-market": "Trouver un commerce déjà sur Tomat",
    "cancel": "Annuler",
    "no-associated-merchant": "Tous les commerces disponibles sont associés au programme.",
    "selected-market-group": "Groupe de commerces",
    "no-results-found": "Aucun commerce trouvé",
    "warning-message":"Attention : Cette liste contient des commerces provenant d’autres programmes qui utilisent Tomat. L'ajout de l'un de ces commerces à votre programme leur permettra d'accepter vos cartes et créera également une nouvelle caisse qui devra être sélectionnée lorsqu'ils ouvriront Tomat. Veuillez procéder avec prudence."
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="returnRoute()" :title="t('title')" :has-footer="false">
    <p class="text-sm text-gray-500">{{ t("warning-message") }}</p>
    <Form v-if="!loadingMarkets && !loadingProject && !loadingMarketGroups"
      v-slot="{ isSubmitting, errors: formErrors }" :validation-schema="validationSchema"
      :initial-values="initialValues" keep-values @submit="onSubmit">
      <PfForm has-footer can-cancel :disable-submit="Object.keys(formErrors).length > 0" :submit-label="t('add-market')"
        :cancel-label="t('cancel')" :processing="isSubmitting" @cancel="closeModal">
        <div>
          <div class="flex flex-col gap-y-6">
            <PfFormSection v-if="filteredMarketOptions.length > 0">
              <Field v-slot="{ field: inputField, errors: fieldErrors }" name="market">
                <PfFormInputSelectSearchable id="marketId" required v-bind="inputField"
                  :placeholder="t('choose-market')" :label="t('select-market')"
                  :no-results-found="t('no-results-found')" :options="filteredMarketOptions" :errors="fieldErrors" />
              </Field>
              <Field v-if="marketGroup" v-slot="{ errors: fieldErrors }" name="marketGroup">
                <PfFormInputSelect id="marketGroup" required :value="marketGroup.id" :label="t('selected-market-group')"
                  :options="marketGroups" disabled :errors="fieldErrors" />
              </Field>
              <Field v-else v-slot="{ field, errors: fieldErrors }" name="marketGroup">
                <PfFormInputSelect id="marketGroup" required v-bind="field" :label="t('selected-market-group')"
                  :options="marketGroups" :disabled="isMarketGroupSelectionDisabled" :errors="fieldErrors" />
              </Field>
            </PfFormSection>
            <template v-else>
              <div class="text-red-500">
                <p class="text-sm">{{ t("no-associated-merchant") }}</p>
              </div>
            </template>
            <PfButtonAction v-if="!isMarketGroupSelectionDisabled || !marketGroup" btn-style="dash" has-icon-left
              type="button" :label="t('create-market')" @click="createMarket" />
          </div>
        </div>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useMutation, useQuery, useResult } from "@vue/apollo-composable";
import { string, object } from "yup";
import { storeToRefs } from "pinia";

import { useNotificationsStore } from "@/lib/store/notifications";
import { useAuthStore } from "@/lib/store/auth";

import { USER_TYPE_MARKETGROUPMANAGER } from "@/lib/consts/enums";
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
const { userType } = storeToRefs(useAuthStore());
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();
const marketGroups = ref([]);

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

const { result: resultMarketGroups, loading: loadingMarketGroups } = useQuery(
  gql`
    query MarketGroups {
      marketGroups {
        id
        name
        markets {
          id
          name
        }
      }
    }
  `
);

const project = useResult(resultProject, null, (data) => {
  if (route.params.projectId) {
    const project = data.projects.find((project) => project.id === route.params.projectId);
    marketGroups.value = project.marketGroups.map((marketGroup) => ({
      value: marketGroup.id,
      label: marketGroup.name
    }));
    return project;
  }

  marketGroups.value = data.projects[0].marketGroups.map((marketGroup) => ({
    value: marketGroup.id,
    label: marketGroup.name
  }));
  return data.projects[0];
});

const marketGroup = useResult(resultMarketGroups, null, (data) => {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) {
    if (route.params.marketGroupId) {
      return data.marketGroups.find((marketGroup) => marketGroup.id === route.params.marketGroupId);
    }

    marketGroups.value = [
      {
        value: data.marketGroups[0].id,
        label: data.marketGroups[0].name
      }
    ];
    return data.marketGroups[0];
  }
  return null;
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

const initialValues = computed(() => {
  return {
    market: null,
    marketGroup:
      route.params.marketGroupId !== null && route.params.marketGroupId !== undefined
        ? route.params.marketGroupId
        : marketGroup != null && marketGroup.value !== null
          ? marketGroup.value.id
          : null
  };
});

const filteredMarketOptions = computed(() => {
  if (!markets.value || (!project.value && !marketGroup.value)) return [];
  if (project.value) {
    return markets.value
      .filter((x) => !project.value.markets.some((y) => y.id === x.value))
      .sort((a, b) => a.label.localeCompare(b.label));
  }
  if (marketGroup.value) {
    return markets.value
      .filter((x) => !marketGroup.value.markets.some((y) => y.id === x.value))
      .sort((a, b) => a.label.localeCompare(b.label));
  }
  return [];
});

const validationSchema = computed(() =>
  object({
    market: string().label(t("select-market")).required(),
    marketGroup: string().label(t("selected-market-group")).required()
  })
);

const isMarketGroupSelectionDisabled = computed(() => route.name === URL_ADD_MERCHANTS_FROM_MARKET_GROUP);

function createMarket() {
  router.push({ name: URL_MARKET_OVERVIEW_ADD });
}

async function onSubmit(values) {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) {
    await addMarketToMarketGroup({
      input: {
        marketGroupId: values.marketGroup,
        marketId: values.market
      }
    });
  } else {
    await addMarketToProject({
      input: {
        projectId: project.value.id,
        marketId: values.market,
        marketGroupId: values.marketGroup
      }
    });
  }
  router.push(returnRoute());
  const marketName = markets.value?.find((m) => m.value === values.market)?.label ?? "";
  addSuccess(t("add-market-success-notification", { marketName }));
}

function returnRoute() {
  if (route.name === URL_ADD_MERCHANTS_FROM_MARKET_GROUP) return { name: URL_MARKET_GROUP_MANAGE_MERCHANTS };
  if (route.name === URL_ADD_MERCHANTS_FROM_PROJECT) return { name: URL_PROJECT_MANAGE_MERCHANTS };
  if (route.name === URL_MARKET_OVERVIEW_SELECT) return { name: URL_MARKET_OVERVIEW };
  else return { name: URL_MARKET_ADMIN };
}
</script>

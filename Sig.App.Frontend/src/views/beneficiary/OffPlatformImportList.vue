<i18n>
{
	"en": {
		"title-step-1": "Before importing your list",
    "title-step-2": "Import participants",
    "title-step-3": "Confirm import of participants",
    "import-beneficiaries-success-notification": "Import of {count} participants was successful.",
    "import-beneficiairies-empty-list-notification": "Import did not work. Please check your file before trying again.",
    "import-beneficiairies-duplicate-uniqueid-notification": "The import of participants could not be completed. Several participants have the same Unique Id 1.",
    "warning-import": "Warning: This import will overwrite the information currently in the platform.",
    "error-summary": "An error prevents importing your file | {count} errors prevent importing your file",
    "error-row": "Row {row}:",
    "mandatory-field": "Field {fieldName} is mandatory.",
    "step-1-confirm": "I understand, continue",
    "step-1-cancel": "Cancel",
    "step-1-confirmation-text-1": "Importing will overwrite existing data and",
    "step-1-confirmation-text-2": "deactivate participants that are not in the file.",
    "step-2-cancel": "Cancel",
    "step-3-cancel": "Back",
    "step-3-confirm": "Confirm",
    "forecast-added-beneficiaries": "Added",
    "forecast-modified-beneficiaries": "Modified",
    "forecast-missing-beneficiaries-line-1": "Missing",
    "forecast-missing-beneficiaries-line-2": "(became inactive)"
	},
	"fr": {
		"title-step-1": "Avant d'importer votre liste",
    "title-step-2": "Importer des participant-e-s",
    "title-step-3": "Confirmer l'importation des participant-e-s",
    "import-beneficiaries-success-notification": "L'importation des {count} participant-e-s a été un succès.",
    "import-beneficiairies-empty-list-notification": "L'importation n'a pas fonctionné. Veuillez vérifier votre fichier avant de réessayer.",
    "import-beneficiairies-duplicate-uniqueid-notification": "L'importation n'a pu être complétée. Plusieurs participant-e-s possèdent le même Id 1.",
    "warning-import": "Avertissement: Cet import écrasera les informations actuellement dans la plateforme.",
    "error-summary": "Une erreur empêche d'importer votre fichier | {count} erreurs empêchent d'importer votre fichier",  
    "error-row": "Ligne {row}:",
    "mandatory-field": "Le champ {fieldName} est obligatoire.",
    "step-1-confirm": "J'ai compris, continuer",
    "step-1-cancel": "Annuler",
    "step-1-confirmation-text-1": "L'importation va écraser les données existantes et",
    "step-1-confirmation-text-2": "rendre inactifs les participants qui ne se trouvent pas dans le fichier.",
    "step-2-cancel": "Annuler",
    "step-3-cancel": "Retour",
    "step-3-confirm": "Confirmer",
    "forecast-added-beneficiaries": "Ajouté-e-s",
    "forecast-modified-beneficiaries": "Modifié-e-s",
    "forecast-missing-beneficiaries-line-1": "Manquant-e-s",
    "forecast-missing-beneficiaries-line-2": "(devenu-e-s inactif-ve-s)"
	}
}
</i18n>

<template>
  <UiDialogWarningModal
    v-if="currentStep === 0"
    :return-route="{ name: URL_BENEFICIARY_ADMIN }"
    :title="title"
    @confirm="nextStep">
    <template #description>
      <p class="mb-0 text-h3 font-bold text-primary-500">
        {{ t("step-1-confirmation-text-1") }}
        <span class="text-primary-900"> {{ t("step-1-confirmation-text-2") }}</span>
      </p>
    </template>

    <PfButtonAction btn-style="link" :label="t('step-1-cancel')" @click="closeModal" />
    <PfButtonAction :label="t('step-1-confirm')" @click="nextStep" />
  </UiDialogWarningModal>

  <UiDialogModal
    v-else
    :return-route="{ name: URL_BENEFICIARY_ADMIN }"
    :title="title"
    :warning-message="currentStep === 1 ? t('warning-import') : ''"
    has-footer
    hide-main-btn>
    <template v-if="currentStep === 1">
      <div v-if="errors.length > 0" class="rounded-md bg-red-50 border-l-4 border-red-400 p-4 mb-4">
        <div class="flex">
          <div class="flex-shrink-0 h-5 w-5 flex items-center justify-center">
            <PfIcon class="text-red-400" size="md" :icon="EXCLAMATION_ICON" aria-hidden="true" />
          </div>
          <div class="ml-3">
            <h3 class="text-sm font-semibold text-red-800 my-0">{{ t("error-summary", errors.length) }}</h3>
            <div class="mt-2 text-sm text-red-700">
              <ul role="list" class="mb-0 list-disc pl-5 space-y-1">
                <li v-for="(error, index) in errors" :key="index">
                  <span v-if="error.rowNumber">{{ t("error-row", { row: error.rowNumber }) }}</span>
                  {{ error.errorType }}
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
      <UiSingleFileUpload
        accept="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        target="none"
        @fileUploaded="onFileUploaded" />
    </template>
    <template v-else>
      <ul class="flex gap-x-8 items-start justify-center text-center mb-0 mt-8 md:mt-12 pb-8 border-b border-grey-300">
        <li class="w-1/3">
          <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
          <div class="mb-4 text-green-600 text-d2 font-bold"><span>+</span>{{ forecastImport.addedBeneficiaries }}</div>
          <div class="leading-snug">{{ t("forecast-added-beneficiaries") }}</div>
        </li>
        <li class="w-1/3">
          <div class="mb-4 text-primary-900 text-d2 font-bold">{{ forecastImport.modifiedBeneficiaries }}</div>
          <div class="leading-snug">{{ t("forecast-modified-beneficiaries") }}</div>
        </li>
        <li class="w-1/3">
          <div class="mb-4 text-red-600 text-d2 font-bold">
            <span v-if="forecastImport.missingBeneficiaries > 0"></span>{{ forecastImport.missingBeneficiaries }}
          </div>
          <div class="leading-snug">{{ t("forecast-missing-beneficiaries-line-1") }}</div>
          <div class="leading-snug">{{ t("forecast-missing-beneficiaries-line-2") }}</div>
        </li>
      </ul>
    </template>
    <template #actions>
      <PfButtonAction v-if="currentStep === 1" btn-style="link" :label="t('step-2-cancel')" @click="closeModal" />
      <PfButtonAction v-if="currentStep === 2" btn-style="link" :label="t('step-3-cancel')" @click="backStep" />
      <PfButtonAction v-if="currentStep === 2" :label="t('step-3-confirm')" @click="nextStep" />
    </template>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useApolloClient, useMutation } from "@vue/apollo-composable";
import { formatDate, serverFormat, dateUtc } from "@/lib/helpers/date";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import {
  FIRST_DAY_OF_THE_MONTH,
  FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH,
  FIRST_DAY_OF_THE_WEEK
} from "@/lib/consts/monthly-payment-moment";

import XlsxService from "@/lib/services/xlsx";
import { useOrganizationStore } from "@/lib/store/organization";
import { useNotificationsStore } from "@/lib/store/notifications";

import EXCLAMATION_ICON from "@/lib/icons/exclamation-circle-fill.json";

const FIRSTNAME_KEY = "Prénom/Firstname";
const LASTNAME_KEY = "Nom de famille/Lastname";
const EMAIL_KEY = "Courriel/Email";
const PHONE_KEY = "Téléphone/Phone";
const ADDRESS_KEY = "Adresse/Address";
const POSTALCODE_KEY = "Code postal/Postal code";
const NOTES_KEY = "Notes/Briefing";
const ID1_KEY = "Id 1";
const ID2_KEY = "Id 2";
const START_DATE = "Date début/Start Date";
const PAYMENT_FREQUENCY = "Fréquence versement/Payment Frequency";
const END_DATE = "Date de fin/End date";
const AMOUNT_PREFIX = "Montant - ";

const MONTHLY_PAYMENT = "mensuel";
const BI_MONTHLY_PAYMENT = "bi-mensuel";
const WEEKLY_PAYMENT = "hebdomadaire";

const { t } = useI18n();
const router = useRouter();
const { resolveClient } = useApolloClient();
const client = resolveClient();
const { addSuccess } = useNotificationsStore();
const { currentOrganization } = useOrganizationStore();

const errors = ref([]);
const currentStep = ref(0);
const importItems = ref([]);
const forecastImport = ref({});

const { mutate: importOffPlatformBeneficiariesListInOrganization } = useMutation(
  gql`
    mutation ImportOffPlatformBeneficiariesListInOrganization($input: ImportOffPlatformBeneficiariesListInOrganizationInput!) {
      importOffPlatformBeneficiariesListInOrganization(input: $input) {
        beneficiaries {
          id
          firstname
          lastname
          email
          phone
          address
          notes
          postalCode
          id1
          id2
          startDate
          endDate
          monthlyPaymentMoment
          isActive
          funds {
            id
            amount
            productGroup {
              id
              name
              color
            }
          }
        }
      }
    }
  `
);

function onFileUploaded({ handle }) {
  errors.value = [];
  const toDate = (x) => {
    if (x === undefined || x === null || x === "") return null;

    let separator = "/";
    if (x.indexOf("/") === -1) separator = "-";
    let [day, month, year] = x.split(separator);

    return { value: formatDate(new Date(+year, +month - 1, +day), serverFormat) };
  };

  handle(async (file) => {
    if (file) {
      const data = await XlsxService.readFile(file);
      let items = [];
      let uniqueIds = [];
      for (let i = 0; i < data.length; i++) {
        const firstname = data[i][FIRSTNAME_KEY];
        const lastname = data[i][LASTNAME_KEY];
        const email = data[i][EMAIL_KEY];
        const phone = data[i][PHONE_KEY];
        const address = data[i][ADDRESS_KEY];
        const postalCode = data[i][POSTALCODE_KEY];
        const notes = data[i][NOTES_KEY];
        const id1 = data[i][ID1_KEY];
        const id2 = data[i][ID2_KEY];
        const startDate = data[i][START_DATE];
        const paymentFrequency = data[i][PAYMENT_FREQUENCY];
        const endDate = data[i][END_DATE];

        const funds = Object.keys(data[i])
          .filter((x) => {
            return x.indexOf(AMOUNT_PREFIX) == 0;
          })
          .map((x) => ({
            productGroupName: x.replace(AMOUNT_PREFIX, ""),
            amount: parseFloat(data[i][x])
          }));

        // Add 2 to i to mimic excel sheet row structure
        validateMandatoryField(id1, i + 2, ID1_KEY);
        validateMandatoryField(firstname, i + 2, FIRSTNAME_KEY);
        validateMandatoryField(lastname, i + 2, LASTNAME_KEY);
        validatePaymentFrequencyField(paymentFrequency, i + 2);

        if (id1 && firstname && lastname) {
          items.push({
            firstname: firstname.toString(),
            lastname: lastname.toString(),
            email: email?.toString(),
            phone: phone?.toString(),
            address: address?.toString(),
            postalCode: postalCode?.toString(),
            notes: notes?.toString(),
            id1: id1?.toString(),
            id2: id2?.toString(),
            startDate: startDate != null ? toDate(startDate) : null,
            endDate: toDate(endDate),
            monthlyPaymentMoment: getPaymentFrequencyValue(paymentFrequency),
            funds
          });
          uniqueIds.push(id1);
        }
      }

      if (items.length === 0 && errors.value.length === 0) {
        errors.value.push({
          errorType: t("import-beneficiairies-empty-list-notification")
        });
      } else if ([...new Set(uniqueIds)].length !== uniqueIds.length) {
        errors.value.push({
          errorType: t("import-beneficiairies-duplicate-uniqueid-notification")
        });
      }

      if (errors.value.length === 0) {
        importItems.value = items;

        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);

        let result = await client.query({
          query: gql`
            query ForecastImportOffPlatformBeneficiariesListInOrganization(
              $organizationId: ID!
              $ids1: [String!]
              $endDates: [DateTime!]
            ) {
              forecastImportOffPlatformBeneficiariesListInOrganization(
                organizationId: $organizationId
                ids1: $ids1
                endDates: $endDates
              ) {
                addedBeneficiaries
                modifiedBeneficiaries
                missingBeneficiaries
              }
            }
          `,
          variables: {
            ids1: uniqueIds,
            endDates: items.map((x) => (x.endDate !== null ? dateUtc(x.endDate.value) : tomorrow)),
            organizationId: currentOrganization
          }
        });
        forecastImport.value = result.data.forecastImportOffPlatformBeneficiariesListInOrganization;
        nextStep();
      }
    }
  });
}

const validateMandatoryField = (itemProperty, rowNumber, key) => {
  if (itemProperty === null || itemProperty === undefined || itemProperty?.toString().trim() === "") {
    errors.value.push({
      rowNumber,
      errorType: t("mandatory-field", { fieldName: key })
    });
  }
};

const validatePaymentFrequencyField = (paymentFrequency, rowNumber) => {
  if (
    paymentFrequency !== BI_MONTHLY_PAYMENT &&
    paymentFrequency !== MONTHLY_PAYMENT &&
    paymentFrequency !== WEEKLY_PAYMENT &&
    paymentFrequency !== undefined
  ) {
    errors.value.push({
      rowNumber,
      errorType: t("mandatory-field", { fieldName: PAYMENT_FREQUENCY })
    });
  }
};

const title = computed(() => {
  if (currentStep.value === 0) {
    return t("title-step-1");
  } else if (currentStep.value === 1) {
    return t("title-step-2");
  } else {
    return t("title-step-3");
  }
});

async function nextStep() {
  if (currentStep.value === 2) {
    await importOffPlatformBeneficiariesListInOrganization({
      input: { items: importItems.value, organizationId: currentOrganization }
    });

    addSuccess(t("import-beneficiaries-success-notification", { count: importItems.value.length }));
    router.push({ name: URL_BENEFICIARY_ADMIN });
    return;
  }
  currentStep.value++;
}

function getPaymentFrequencyValue(paymentFrequency) {
  switch (paymentFrequency?.toString()) {
    case MONTHLY_PAYMENT:
      return FIRST_DAY_OF_THE_MONTH;
    case BI_MONTHLY_PAYMENT:
      return FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH;
    case WEEKLY_PAYMENT:
      return FIRST_DAY_OF_THE_WEEK;
  }

  return FIRST_DAY_OF_THE_MONTH;
}

function backStep() {
  currentStep.value--;
}

function closeModal() {
  router.push({ name: URL_BENEFICIARY_ADMIN });
}
</script>

<i18n>
{
	"en": {
		"title": "Import participants",
    "import-beneficiaries-success-notification": "Import of {count} participants was successful.",
    "import-beneficiaries-warning-notification":"Import of {count} participants was successful. However {failedCount} participants could not be imported.",
    "import-beneficiairies-empty-list-notification": "Import did not work. Please check your file before trying again.",
    "import-beneficiairies-duplicate-uniqueid-notification": "The import of participants could not be completed. Several participants have the same Unique Id 1.",
    "warning-import": "Warning: This import will overwrite the information currently in the platform.",
    "error-summary": "An error prevents importing your file | {count} errors prevent importing your file",
    "error-row": "Row {row}:",
    "mandatory-field": "Field {fieldName} is mandatory.",
    "invalid-email": "Email ({email}) is invalid.",
    "beneficiary-type-not-found": "Import did not work. One or more categories are missing in the platform.",
    "invalid-postal-code": "Postal code ({postalCode}) is invalid."
	},
	"fr": {
		"title": "Importer des participant-e-s",
    "import-beneficiaries-success-notification": "L'importation des {count} participant-e-s a été un succès.",
    "import-beneficiaries-warning-notification": "L'importation de {count} participant-e-s a été un succès. Par contre {failedCount} participant-e-s n'ont pas pu être importé-e-s.",
    "import-beneficiairies-empty-list-notification": "L'importation n'a pas fonctionné. Veuillez vérifier votre fichier avant de réessayer.",
    "import-beneficiairies-duplicate-uniqueid-notification": "L'importation n'a pu être complétée. Plusieurs participant-e-s possèdent le même Id 1.",
    "warning-import": "Avertissement: Cet import va mettre à jour les informations actuellement dans la plateforme.",
    "error-summary": "Une erreur empêche d'importer votre fichier | {count} erreurs empêchent d'importer votre fichier",  
    "error-row": "Ligne {row}:",
    "mandatory-field": "Le champ {fieldName} est obligatoire.",
    "invalid-email": "Le courriel ({email}) est invalide.",
    "beneficiary-type-not-found": "L'importation n'a pu être complétée. Une ou plusieurs catégories sont manquantes dans la plateforme.",
    "invalid-postal-code": "Le code postal ({postalCode}) est invalide."
	}
}
</i18n>

<template>
  <UiDialogModal :return-route="{ name: URL_BENEFICIARY_ADMIN }" :title="t('title')" :warning-message="t('warning-import')">
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
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useOrganizationStore } from "@/lib/store/organization";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import XlsxService from "@/lib/services/xlsx";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import UiDialogModal from "@/components/ui/dialog/modal.vue";

import EXCLAMATION_ICON from "@/lib/icons/exclamation-circle-fill.json";

useGraphQLErrorMessages({
  BENEFICIARY_TYPE_NOT_FOUND: () => {
    return t("beneficiary-type-not-found");
  }
});

const FIRSTNAME_KEY = "Prénom/Firstname";
const LASTNAME_KEY = "Nom de famille/Lastname";
const PHONE_KEY = "Téléphone/Phone";
const EMAIL_KEY = "Courriel/Email";
const ADDRESS_KEY = "Adresse/Address";
const POSTALCODE_KEY = "Code postal/Postal code";
const NOTES_KEY = "Notes/Briefing";
const CATEGORY_KEY = "Catégorie/Category";
const ID1_KEY = "Id 1";
const ID2_KEY = "Id 2";

const { t } = useI18n();
const router = useRouter();
const { addSuccess, addInfo } = useNotificationsStore();
const { currentOrganization } = useOrganizationStore();

const errors = ref([]);

const { mutate: importBeneficiariesListInOrganization } = useMutation(
  gql`
    mutation ImportBeneficiariesListInOrganization($input: ImportBeneficiariesListInOrganizationInput!) {
      importBeneficiariesListInOrganization(input: $input) {
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
          ... on BeneficiaryGraphType {
            beneficiaryType {
              id
              name
            }
          }
        }
      }
    }
  `
);

function onFileUploaded({ handle }) {
  errors.value = [];

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
        const key = data[i][CATEGORY_KEY];
        const id1 = data[i][ID1_KEY];
        const id2 = data[i][ID2_KEY];

        // Add 2 to i to mimic excel sheet row structure
        validateMandatoryField(id1, i + 2, ID1_KEY);
        validateMandatoryField(firstname, i + 2, FIRSTNAME_KEY);
        validateMandatoryField(lastname, i + 2, LASTNAME_KEY);
        validateMandatoryField(key, i + 2, CATEGORY_KEY);
        validateEmailAddress(email, i + 2);
        validatePostalCode(postalCode, i + 2);

        if (id1 && firstname && lastname && key) {
          items.push({
            firstname: firstname.toString(),
            lastname: lastname.toString(),
            email: email?.toString(),
            phone: phone?.toString(),
            address: address?.toString(),
            postalCode: postalCode?.toString(),
            notes: notes?.toString(),
            key: key.toString(),
            id1: id1?.toString(),
            id2: id2?.toString()
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
        await importBeneficiariesListInOrganization({ input: { items, organizationId: currentOrganization } });

        if (data.length !== items.length) {
          addInfo(
            t("import-beneficiaries-warning-notification", { count: items.length, failedCount: data.length - items.length })
          );
        } else {
          addSuccess(t("import-beneficiaries-success-notification", { count: items.length }));
        }
        router.push({ name: URL_BENEFICIARY_ADMIN });
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

const validateEmailAddress = (email, rowNumber) => {
  const emailIsSet = email !== null && email !== undefined;
  const validEmail = !emailIsSet || /^\S+@\S+\.\S+$/.test(email.toString().trim());
  if (!validEmail) {
    errors.value.push({
      rowNumber,
      errorType: t("invalid-email", { email: email })
    });
  }
};

const validatePostalCode = (postalCode, rowNumber) => {
  const postalCodeIsSet = postalCode !== null && postalCode !== undefined && postalCode !== "";
  const validPostalCode =
    !postalCodeIsSet || /^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z][ -]?\d[ABCEGHJ-NPRSTV-Z]\d$/i.test(postalCode);
  if (!validPostalCode) {
    errors.value.push({
      rowNumber,
      errorType: t("invalid-postal-code", { postalCode: postalCode })
    });
  }
};
</script>

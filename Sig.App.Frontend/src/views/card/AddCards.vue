<i18n>
{
	"en": {
		"cancel": "Cancel",
		"card-count": "Number of cards to generate",
		"card-placeholder": "Ex. 40",
    "display-printing-file-xlsx": "See the file (XLSX)",
		"generate-card": "Generate",
		"generate-cards-success-detail": "The generation of {cardCountCreated} cards is completed. The next step is to make a print request for the cards. To access the print files, click on See the file (XLSX).",
		"printing-cards-detail": "In addition, the print file was emailed to program managers.",
		"title": "Generate new cards"
	},
	"fr": {
		"cancel": "Annuler",
		"card-count": "Nombre de carte à générer",
		"card-placeholder": "Ex. 40",
    "display-printing-file-xlsx": "Voir le fichier (XLSX)",
		"generate-card": "Générer",
		"generate-cards-success-detail": "La génération de {cardCountCreated} cartes est complétée. La prochaine étape est de faire une demande d'impression pour les cartes. Pour avoir accès aux identifiants des cartes, cliquer sur Voir le fichier (XLSX).",
		"printing-cards-detail": "De plus, le fichier d'impression a été envoyé aux gestionnaires du programme par courriel.",
		"title": "Générer de nouvelles cartes"
	}
}
</i18n>

<template>
  <UiDialogModal
    :return-route="{ name: URL_CARDS }"
    :title="t('title')"
    :has-footer="createCardSuccess"
    can-cancel
    @cancel="displayPrintingFile">
    <template #actions>
      <PfButtonAction type="button" btn-style="link" :label="t('display-printing-file-xlsx')" @click="displayPrintingFileXlsx" />
    </template>
    <template v-if="createCardSuccess" #default>
      <p class="text-p2">
        {{ t("generate-cards-success-detail", { cardCountCreated }) }}
      </p>
      <p class="text-p2">
        <strong>{{ t("printing-cards-detail") }}</strong>
      </p>
    </template>
    <template v-else #default="{ closeModal }">
      <Form v-slot="{ isSubmitting, errors: formErrors }" :validation-schema="validationSchema" @submit="onSubmit">
        <PfForm
          has-footer
          can-cancel
          :disable-submit="Object.keys(formErrors).length > 0"
          :submit-label="t('generate-card')"
          :cancel-label="t('cancel')"
          :processing="isSubmitting"
          @cancel="closeModal">
          <Field v-slot="{ field, errors: fieldErrors }" name="cardCount">
            <PfFormInputText
              id="cardCount"
              v-bind="field"
              :label="t('card-count')"
              :placeholder="t('card-placeholder')"
              :errors="fieldErrors"
              input-type="number"
              min="0"
              col-span-class="sm:col-span-4" />
          </Field>
        </PfForm>
      </Form>
    </template>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { ref, computed } from "vue";
import { object, number, lazy, string } from "yup";
import { useRoute } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { URL_CARDS } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const createCardSuccess = ref(false);
const cardCountCreated = ref(0);
const cardCreatedXlsxUrl = ref("");

const validationSchema = computed(() =>
  object({
    cardCount: lazy((value) => {
      if (value === "") {
        return string().label(t("card-count")).required();
      }

      return number().label(t("card-count")).required().min(1);
    })
  })
);

const { mutate: createCards } = useMutation(
  gql`
    mutation CreateCards($input: CreateCardsInput!) {
      createCards(input: $input) {
        project {
          id
        }
        xlsxUrl
      }
    }
  `
);

function displayPrintingFileXlsx() {
  window.open(cardCreatedXlsxUrl.value, "_blank").focus();
}

async function onSubmit({ cardCount }) {
  var result = await createCards({
    input: {
      count: parseInt(cardCount),
      projectId: route.query.projectId
    }
  });

  cardCountCreated.value = cardCount;
  cardCreatedXlsxUrl.value = result.data.createCards.xlsxUrl;
  createCardSuccess.value = true;
}
</script>

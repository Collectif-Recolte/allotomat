<i18n>
{
	"en": {
		"add-category": "Add category",
		"add-category-success-notification": "Category {categoryName} has been successfully added.",
		"title": "Add Category",
    "beneficiary-type-key-already-in-use": "One of the keys you've entered is already associated with another category."
	},
	"fr": {
		"add-category": "Ajouter la catégorie",
		"add-category-success-notification": "L'ajout de la catégorie {categoryName} a été un succès.",
		"title": "Ajouter une catégorie",
    "beneficiary-type-key-already-in-use": "L'une des clés que vous avez saisies est déjà associée à une autre catégorie."
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_CATEGORY_ADMIN }" :title="t('title')" :has-footer="false">
    <CategoryForm
      :submit-btn="t('add-category')"
      :project-id="route.query.projectId"
      @closeModal="closeModal"
      @submit="onSubmit" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_CATEGORY_ADMIN } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import CategoryForm from "@/views/category/_Form.vue";

useGraphQLErrorMessages({
  BENEFICIARY_TYPE_KEY_ALREADY_IN_USE: () => {
    return t("beneficiary-type-key-already-in-use");
  }
});

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { mutate: addBeneficiaryTypeInProject } = useMutation(
  gql`
    mutation AddBeneficiaryTypeInProject($input: AddBeneficiaryTypeInProjectInput!) {
      addBeneficiaryTypeInProject(input: $input) {
        beneficiaryType {
          id
          name
          keys
        }
      }
    }
  `
);

async function onSubmit({ categoryName, categoryKeys }) {
  await addBeneficiaryTypeInProject({
    input: {
      projectId: route.query.projectId,
      name: categoryName,
      keys: categoryKeys.map((x) => x.name)
    }
  });
  router.push({ name: URL_CATEGORY_ADMIN });
  addSuccess(t("add-category-success-notification", { categoryName }));
}
</script>

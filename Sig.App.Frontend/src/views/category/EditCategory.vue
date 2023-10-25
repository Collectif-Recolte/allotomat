<i18n>
{
	"en": {
		"add-category": "Edit",
		"add-category-success-notification": "The modification to {categoryName} was successful.",
		"title": "Edit a category",
    "beneficiary-type-key-already-in-use": "An «Association Key» is already in use in another category."
	},
	"fr": {
		"add-category": "Modifier",
		"add-category-success-notification": "La modification de la catégorie {categoryName} a été un succès.",
		"title": "Modifier une catégorie",
    "beneficiary-type-key-already-in-use": "Une «Clé d'association» est déjà utilisée pour une autre catégorie."
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_CATEGORY_ADMIN }" :title="t('title')" :has-footer="false">
    <CategoryForm
      v-if="category"
      :submit-btn="t('add-category')"
      :project-id="category.project.id"
      :category-name="category.name"
      :category-keys="categoryKeys"
      @closeModal="closeModal"
      @submit="onSubmit" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

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

const { result } = useQuery(
  gql`
    query BeneficiaryType($id: ID!) {
      beneficiaryType(id: $id) {
        id
        name
        keys
        project {
          id
        }
      }
    }
  `,
  {
    id: route.params.categoryId
  }
);
let category = useResult(result);

const categoryKeys = computed(() => {
  return category.value.keys.map((x) => ({ name: x }));
});

const { mutate: editBeneficiaryType } = useMutation(
  gql`
    mutation EditBeneficiaryType($input: EditBeneficiaryTypeInput!) {
      editBeneficiaryType(input: $input) {
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
  await editBeneficiaryType({
    input: {
      beneficiaryTypeId: route.params.categoryId,
      name: { value: categoryName },
      keys: categoryKeys.map((x) => x.name)
    }
  });
  router.push({ name: URL_CATEGORY_ADMIN });
  addSuccess(t("add-category-success-notification", { categoryName }));
}
</script>

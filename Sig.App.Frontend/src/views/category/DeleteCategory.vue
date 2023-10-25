<i18n>
{
	"en": {
		"delete-category-success-notification": "The category {categoryName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the category",
		"delete-text-label": "Type the name of the category to confirm",
		"description": "Warning ! The deletion of <strong>{categoryName}</strong> cannot be undone. If you continue, the category will be deleted.",
		"title": "Delete - {categoryName}"
	},
	"fr": {
		"delete-category-success-notification": "La catégorie {categoryName} a été supprimée avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom de la catégorie",
		"delete-text-label": "Taper le nom de la catégorie pour confirmer",
		"description": "Avertissement ! La suppression de <strong>{categoryName}</strong> ne peut pas être annulée. Si vous continuez, la catégorie sera supprimée.",
		"title": "Supprimer - {categoryName}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_CATEGORY_ADMIN }"
    :title="t('title', { categoryName: getCategoryName() })"
    :description="t('description', { categoryName: getCategoryName() })"
    :validation-text="getCategoryName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteCategory" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_CATEGORY_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result: resultCategory } = useQuery(
  gql`
    query BeneficiaryType($id: ID!) {
      beneficiaryType(id: $id) {
        id
        name
      }
    }
  `,
  {
    id: route.params.categoryId
  }
);
const category = useResult(resultCategory);

const { mutate: deleteBeneficiaryTypeMutation } = useMutation(
  gql`
    mutation DeleteBeneficiaryType($input: DeleteBeneficiaryTypeInput!) {
      deleteBeneficiaryType(input: $input)
    }
  `
);

function getCategoryName() {
  return category.value ? category.value.name : "";
}

async function deleteCategory() {
  await deleteBeneficiaryTypeMutation({
    input: {
      beneficiaryTypeId: route.params.categoryId
    }
  });

  addSuccess(t("delete-category-success-notification", { categoryName: category.value.name }));
  router.push({ name: URL_CATEGORY_ADMIN });
}
</script>

<i18n>
{
	"en": {
		"delete-product-group-success-notification": "The product group {productGroupName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the product group",
		"delete-text-label": "Type the name of the product group to confirm",
		"description": "Warning ! The deletion of <strong>{productGroupName}</strong> cannot be undone. If you continue, the product group will be deleted.",
		"title": "Delete - {productGroupName}"
	},
	"fr": {
		"delete-product-group-success-notification": "Le groupe de produits {productGroupName} a été supprimé avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom du groupe de produits",
		"delete-text-label": "Taper le nom du groupe de produits pour confirmer",
		"description": "Avertissement ! La suppression de <strong>{productGroupName}</strong> ne peut pas être annulée. Si vous continuez, le groupe de produits sera supprimé.",
		"title": "Supprimer - {productGroupName}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_PRODUCT_GROUP_ADMIN }"
    :title="t('title', { productGroupName: getProductGroupName() })"
    :description="t('description', { productGroupName: getProductGroupName() })"
    :validation-text="getProductGroupName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteProductGroup" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_PRODUCT_GROUP_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result: resultProductGroup } = useQuery(
  gql`
    query ProductGroup($id: ID!) {
      productGroup(id: $id) {
        id
        name
      }
    }
  `,
  {
    id: route.params.productGroupId
  }
);
const productGroup = useResult(resultProductGroup);

const { mutate: deleteProductGroupMutation } = useMutation(
  gql`
    mutation DeleteProductGroup($input: DeleteProductGroupInput!) {
      deleteProductGroup(input: $input)
    }
  `
);

function getProductGroupName() {
  return productGroup.value ? productGroup.value.name : "";
}

async function deleteProductGroup() {
  await deleteProductGroupMutation({
    input: {
      productGroupId: route.params.productGroupId
    }
  });

  addSuccess(t("delete-product-group-success-notification", { productGroupName: productGroup.value.name }));
  router.push({ name: URL_PRODUCT_GROUP_ADMIN });
}
</script>

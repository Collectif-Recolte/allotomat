<i18n>
{
	"en": {
		"add-product-group": "Edit",
		"add-product-group-success-notification": "The modification to {productGroupName} was successful.",
		"title": "Edit a product group"
	},
	"fr": {
		"add-product-group": "Modifier",
		"add-product-group-success-notification": "La modification du groupe de produits {productGroupName} a été un succès.",
		"title": "Modifier un groupe de produits"
	}
}
</i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :return-route="{ name: URL_PRODUCT_GROUP_ADMIN }"
    :title="t('title')"
    :has-footer="false">
    <ProductGroupForm
      v-if="productGroup"
      :submit-btn="t('add-product-group')"
      :project-id="productGroup.project.id"
      :product-group-name="productGroup.name"
      :product-group-order="productGroup.orderOfAppearance"
      :product-group-color="productGroup.color"
      @closeModal="closeModal"
      @submit="onSubmit" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_PRODUCT_GROUP_ADMIN } from "@/lib/consts/urls";

import ProductGroupForm from "@/views/product-groups/_Form.vue";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query ProductGroup($id: ID!) {
      productGroup(id: $id) {
        id
        name
        color
        orderOfAppearance
        project {
          id
        }
      }
    }
  `,
  {
    id: route.params.productGroupId
  }
);
let productGroup = useResult(result);

const { mutate: editProductGroup } = useMutation(
  gql`
    mutation EditProductGroup($input: EditProductGroupInput!) {
      editProductGroup(input: $input) {
        productGroup {
          id
          name
          color
          orderOfAppearance
        }
      }
    }
  `
);

async function onSubmit({ productGroupName, productGroupColor, productGroupOrder }) {
  await editProductGroup({
    input: {
      productGroupId: route.params.productGroupId,
      name: { value: productGroupName },
      color: { value: productGroupColor },
      orderOfAppearance: { value: parseInt(productGroupOrder) }
    }
  });
  router.push({ name: URL_PRODUCT_GROUP_ADMIN });
  addSuccess(t("add-product-group-success-notification", { productGroupName }));
}
</script>

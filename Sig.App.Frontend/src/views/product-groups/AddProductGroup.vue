<i18n>
{
	"en": {
		"add-product-group": "Add product group",
		"add-product-group-success-notification": "Adding product group {productGroupName} was successful.",
		"title": "Add a product group"
	},
	"fr": {
		"add-product-group": "Ajouter le groupe de produits",
		"add-product-group-success-notification": "L’ajout du groupe de produits {productGroupName} a été un succès.",
		"title": "Ajouter un groupe de produits"
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
      :submit-btn="t('add-product-group')"
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
import { URL_PRODUCT_GROUP_ADMIN } from "@/lib/consts/urls";

import ProductGroupForm from "@/views/product-groups/_Form.vue";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { mutate: createProductGroup } = useMutation(
  gql`
    mutation CreateProductGroup($input: CreateProductGroupInput!) {
      createProductGroup(input: $input) {
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
  await createProductGroup({
    input: {
      projectId: route.query.projectId,
      name: productGroupName,
      color: productGroupColor,
      orderOfAppearance: parseInt(productGroupOrder)
    }
  });
  router.push({ name: URL_PRODUCT_GROUP_ADMIN });
  addSuccess(t("add-product-group-success-notification", { productGroupName }));
}
</script>

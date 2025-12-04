<i18n>
{
	"en": {
		"cancel": "Cancel",
		"product-group-name": "Product group name",
		"product-group-name-placeholder": "Ex. Fruits",
    "product-group-name-description": "Will be displayed in French and English.",
    "product-group-order": "Display order for the market",
		"product-group-order-placeholder": "Ex. 4",
    "product-group-order-description": "A lower number will be displayed first in the merchant's list.",
    "product-group-color": "Color"
	},
	"fr": {
		"cancel": "Annuler",
		"product-group-name": "Nom du groupe",
		"product-group-name-placeholder": "Ex. Fruits",
    "product-group-name-description": "Sera affiché en français et en anglais.",
    "product-group-order": "Ordre d’affichage pour le commerçant",
		"product-group-order-placeholder": "Ex. 4",
    "product-group-order-description": "Un chiffre moins élevé sera affiché en premier dans la liste du commerçant.",
    "product-group-color": "Couleur"
	}
}
</i18n>

<template>
  <Form v-slot="{ isSubmitting, meta }" :validation-schema="validationSchema" :initial-values="initialValues" @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="!meta.valid"
      :submit-label="props.submitBtn"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      @cancel="closeModal">
      <PfFormSection is-grid>
        <Field v-slot="{ field, errors: fieldErrors }" name="productGroupName">
          <PfFormInputText
            id="productGroupName"
            :model-value="field.value"
            :label="t('product-group-name')"
            :placeholder="t('product-group-name-placeholder')"
            :description="t('product-group-name-description')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-12"
            @update:modelValue="field.onChange" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="productGroupOrder">
          <PfFormInputText
            id="productGroupOrder"
            :model-value="field.value"
            input-type="number"
            input-mode="numeric"
            :min="0"
            :label="t('product-group-order')"
            :placeholder="t('product-group-order-placeholder')"
            :description="t('product-group-order-description')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-6"
            @update:modelValue="field.onChange" />
        </Field>

        <div class="relative z-10 sm:col-span-6 mb-16">
          <Field v-slot="{ field }" name="productGroupColor">
            <UiSelectColor
              :model-value="field.value"
              :label="t('product-group-color')"
              :options="getColorList()"
              @update:modelValue="field.onChange" />
          </Field>
        </div>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { defineEmits, defineProps, computed } from "vue";
import { object, string, number, lazy } from "yup";
import { getColorList } from "@/lib/helpers/products-color";

const { t } = useI18n();

const emit = defineEmits(["submit", "closeModal"]);
const props = defineProps({
  title: {
    type: String,
    default: ""
  },
  submitBtn: {
    type: String,
    default: ""
  },
  productGroupName: {
    type: String,
    default: ""
  },
  productGroupOrder: {
    type: Number,
    default: null
  },
  productGroupColor: {
    type: String,
    default: getColorList()[0].value
  },
  projectId: {
    type: String,
    required: true
  }
});

const initialValues = {
  productGroupName: props.productGroupName,
  productGroupOrder: props.productGroupOrder,
  productGroupColor: props.productGroupColor
};

const validationSchema = computed(() =>
  object({
    productGroupName: string().label(t("product-group-name")).required(),
    productGroupOrder: lazy(() => {
      return number()
        .label(t("product-group-order"))
        .transform((value) => (isNaN(value) ? undefined : value))
        .required()
        .min(0);
    })
  })
);

function closeModal() {
  emit("closeModal");
}

async function onSubmit({ productGroupName, productGroupColor, productGroupOrder }) {
  emit("submit", { productGroupName, productGroupColor, productGroupOrder });
}
</script>

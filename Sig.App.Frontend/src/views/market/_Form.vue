<i18n>
{
	"en": {
		"cancel": "Cancel",
		"market-name": "Name",
		"market-name-placeholder": "Ex. Central market",
    "selected-market-group": "Location"
	},
	"fr": {
		"cancel": "Annuler",
		"market-name": "Nom",
		"market-name-placeholder": "Ex. Marché centrale",
    "selected-market-group": "Lieu"
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, meta }"
    :validation-schema="validationSchema || baseValidationSchema"
    :initial-values="initialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="meta.valid === false"
      :submit-label="props.submitBtn"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="marketName">
          <PfFormInputText
            id="marketName"
            :model-value="field.value"
            :label="t('market-name')"
            :placeholder="t('market-name-placeholder')"
            :errors="fieldErrors"
            @update:modelValue="field.onChange" />
        </Field>
        <Field v-if="isInProject" v-slot="{ field, errors: fieldErrors }" name="marketGroup">
          <PfFormInputSelect
            id="marketGroup"
            :model-value="field.value"
            :label="t('selected-market-group')"
            :options="marketGroupOptions"
            :errors="fieldErrors"
            @update:modelValue="field.onChange" />
        </Field>
      </PfFormSection>
      <slot></slot>
    </PfForm>
  </Form>
</template>

<script setup>
import { defineEmits, defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";

const { t } = useI18n();
const emit = defineEmits(["submit", "closeModal"]);

const props = defineProps({
  submitBtn: {
    type: String,
    default: ""
  },
  marketName: {
    type: String,
    default: ""
  },
  initialValues: {
    type: Object,
    default(rawProps) {
      return { marketName: rawProps.marketName, marketGroup: "" };
    }
  },
  validationSchema: {
    type: Object,
    default: null
  },
  isNew: {
    type: Boolean,
    default: false
  },
  isInProject: {
    type: Boolean,
    default: false
  },
  marketGroupOptions: {
    type: Array,
    default() {
      return [];
    }
  }
});

const baseValidationSchema = computed(() =>
  object({
    marketName: string().label(t("market-name")).required()
  })
);

function closeModal() {
  emit("closeModal");
}

async function onSubmit(values) {
  emit("submit", values);
}
</script>

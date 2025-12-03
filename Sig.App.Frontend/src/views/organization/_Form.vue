<i18n>
{
	"en": {
		"cancel": "Cancel",
		"organization-name": "Name",
		"organization-name-placeholder": "Ex. The Carrefour"
	},
	"fr": {
		"cancel": "Annuler",
		"organization-name": "Nom",
		"organization-name-placeholder": "Ex. Carrefour solidaire"
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, meta }"
    :validation-schema="validationSchema || baseValidationSchema"
    :initial-values="initialValues || baseInitialValues"
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
        <Field v-slot="{ field, errors: fieldErrors }" name="name">
          <PfFormInputText
            id="name"
            :model-value="field.value"
            :label="t('organization-name')"
            :placeholder="t('organization-name-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4"
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
  title: {
    type: String,
    default: ""
  },
  submitBtn: {
    type: String,
    default: ""
  },
  name: {
    type: String,
    default: ""
  },
  initialValues: {
    type: Object,
    default: null
  },
  validationSchema: {
    type: Object,
    default: null
  }
});

const baseInitialValues = {
  name: props.name
};

const baseValidationSchema = computed(() =>
  object({
    name: string().label(t("organization-name")).required()
  })
);

function closeModal() {
  emit("closeModal");
}

async function onSubmit(values) {
  emit("submit", values);
}
</script>

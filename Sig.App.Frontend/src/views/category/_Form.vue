<i18n>
{
	"en": {
		"add-keyword": "Add a key",
		"cancel": "Cancel",
		"empty-keyword-list-error": "The list «Association keys» must contain at least 1 element",
		"category-name": "Category name",
		"category-placeholder": "Ex. Type couple",
		"keyword-name": "Key",
		"keyword-name-placeholder": "Ex. couple",
		"keywords-section-title": "Association keys",
	},
	"fr": {
		"add-keyword": "Ajouter une clé",
		"cancel": "Annuler",
		"empty-keyword-list-error": "La liste «Clés d'association» doit contenir au moins 1 élément",
		"category-name": "Nom de la catégorie",
		"category-placeholder": "Ex. Type couple",
		"keyword-name": "Clé",
		"keyword-name-placeholder": "Ex. couple",
		"keywords-section-title": "Clés d'association",
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, errors: formErrors }"
    :validation-schema="validationSchema"
    :initial-values="initialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="props.submitBtn"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="categoryName">
          <PfFormInputText
            id="categoryName"
            v-bind="field"
            :label="t('category-name')"
            :placeholder="t('category-placeholder')"
            :errors="fieldErrors" />
        </Field>
      </PfFormSection>

      <PfFormSection :title="t('keywords-section-title')">
        <FieldArray v-slot="{ fields, remove, push }" key-path="id" name="categoryKeys">
          <UiFieldArray
            :fields="fields"
            :add-label="t('add-keyword')"
            :empty-list-error="t('empty-keyword-list-error')"
            @addField="() => push({ id: uniqueId(), name: '' })"
            @removeField="(idx) => remove(idx)">
            <template #default="slotProps">
              <Field v-slot="{ field: inputField, errors: fieldErrors }" :name="`categoryKeys[${slotProps.idx}].name`">
                <PfFormInputText
                  :id="`categoryKeys[${slotProps.idx}].name`"
                  class="grow"
                  v-bind="inputField"
                  :label="t('keyword-name')"
                  :placeholder="t('keyword-name-placeholder')"
                  :errors="fieldErrors"
                  col-span-class="col-span-3" />
              </Field>
            </template>
          </UiFieldArray>
        </FieldArray>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { defineEmits, defineProps, computed } from "vue";
import { FieldArray } from "vee-validate";
import { string, object, array } from "yup";
import { uniqueId } from "@/lib/helpers/id-helpers";

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
  categoryName: {
    type: String,
    default: ""
  },
  categoryKeys: {
    type: Array,
    default() {
      return [{ name: "" }];
    }
  },
  projectId: {
    type: String,
    required: true
  }
});

const initialValues = {
  categoryName: props.categoryName,
  categoryKeys: props.categoryKeys.map((x) => ({
    id: uniqueId(),
    name: x.name
  }))
};

const validationSchema = computed(() =>
  object({
    categoryName: string().label(t("category-name")).required(),
    categoryKeys: array().of(
      object({
        name: string().label(t("keyword-name")).required()
      })
    )
  })
);

function closeModal() {
  emit("closeModal");
}

async function onSubmit({ categoryName, categoryKeys }) {
  emit("submit", { categoryName, categoryKeys });
}
</script>

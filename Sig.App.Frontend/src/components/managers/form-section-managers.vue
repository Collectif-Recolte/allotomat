<i18n>
    {
        "en": {
            "add-manager": "Add manager",
            "empty-managers-list-error": "The list of «Manager» must contain at least 1 element",
            "manager-email": "Email",
            "manager-placeholder": "Ex. john.doe{'@'}example.com",
            "managers-section-title": "Managers"
        },
        "fr": {
            "add-manager": "Ajouter un gestionnaire",
            "empty-managers-list-error": "La liste de «Gestionnaire» doit contenir au moins 1 élément",
            "manager-email": "Courriel",
            "manager-placeholder": "Ex. john.doe{'@'}exemple.com",
            "managers-section-title": "Gestionnaires"
        }
    }
</i18n>

<template>
  <PfFormSection :title="t('managers-section-title')">
    <FieldArray v-slot="{ fields, remove, push }" key-path="id" name="managers">
      <UiFieldArray
        :fields="fields"
        :add-label="t('add-manager')"
        :empty-list-error="t('empty-managers-list-error')"
        @addField="() => push({ id: fields.length, url: '', name: '' })"
        @removeField="(idx) => remove(idx)">
        <template #default="slotProps">
          <Field v-slot="{ field: inputField, errors: fieldErrors }" :name="`managers[${slotProps.idx}].email`">
            <PfFormInputText
              :id="`managers[${slotProps.idx}].email`"
              class="grow"
              :model-value="inputField.value"
              :label="t('manager-email')"
              :placeholder="t('manager-placeholder')"
              :errors="fieldErrors"
              col-span-class="col-span-3"
              @update:modelValue="inputField.onChange" />
          </Field>
        </template>
      </UiFieldArray>
    </FieldArray>
  </PfFormSection>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { FieldArray } from "vee-validate";

const { t } = useI18n();
</script>

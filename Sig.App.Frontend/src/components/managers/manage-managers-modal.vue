<i18n>
  {
    "en": {
      "add-button-label": "Add a manager",
      "add-manager": "Add",
      "email": "Courriel",
      "email-placeholder": "john.doe{'@'}example.com",
      "no-associated-managers": "No associated manager",
    },
    "fr": {
      "add-button-label": "Ajouter un gestionnaire",
      "add-manager": "Ajouter",
      "email": "Courriel",
      "email-placeholder": "john.doe{'@'}example.com",
      "no-associated-managers": "Aucun gestionnaire associé",
    }
  }
</i18n>

<template>
  <UiDialogModal :title="props.title" :return-route="returnRoute">
    <div v-if="managers.length === 0" class="text-red-500">
      <p class="text-sm">{{ t("no-associated-managers") }}</p>
    </div>
    <ManagersTable v-else :managers="managers" @removeManager="removeManager" />
    <PfButtonAction
      v-if="!addManagerDisplayed"
      type="button"
      btn-style="dash"
      class="w-full"
      :label="t('add-button-label')"
      has-icon-left
      :icon="ICON_PLUS"
      @click="displayAddManager()" />
    <Form v-else v-slot="{ isSubmitting, errors: formErrors }" :validation-schema="validationSchema" @submit="addManager">
      <PfFormNested
        can-cancel
        :disable-submit="Object.keys(formErrors).length > 0"
        :submit-label="t('add-manager')"
        :processing="isSubmitting"
        @cancel="addManagerDisplayed = false">
        <template #lastField>
          <Field v-slot="{ field, errors: fieldErrors }" name="email">
            <PfFormInputText
              id="email"
              v-bind="field"
              :label="t('email')"
              :placeholder="t('email-placeholder')"
              :errors="fieldErrors"
              col-span-class="sm:col-span-6" />
          </Field>
        </template>
      </PfFormNested>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import { defineProps, defineEmits, defineExpose, ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { object, string } from "yup";

import ICON_PLUS from "@/lib/icons/plus.json";
import ManagersTable from "@/components/managers/managers-table";

const { t } = useI18n();

const props = defineProps({
  title: { type: String, default: null },
  managers: {
    type: Array,
    default: () => {
      return [];
    }
  },
  returnRoute: { type: Object, default: null }
});

const addManagerDisplayed = ref(false);

const validationSchema = computed(() =>
  object({
    email: string().label(t("email")).required().email()
  })
);

defineExpose({
  hideAddManagerForm: () => (addManagerDisplayed.value = false)
});

const emit = defineEmits(["removeManager", "addManager"]);

const removeManager = (manager) => {
  emit("removeManager", manager);
};

const displayAddManager = () => {
  addManagerDisplayed.value = true;
};

const addManager = ({ email }) => {
  emit("addManager", email);
};
</script>

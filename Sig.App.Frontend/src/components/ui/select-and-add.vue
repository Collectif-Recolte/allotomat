<i18n>
  {
    "en": {
      "select-placeholder": "Select",
      "save": "Add"
    },
    "fr": {
      "select-placeholder": "Sélectionner",
      "save": "Ajouter"
    }
  }
</i18n>

<template>
  <Form v-if="showSelect" :validation-schema="validationSchema" @submit="onSubmit">
    <PfForm>
      <PfFormSection>
        <div class="xs:flex xs:gap-x-4">
          <Field v-slot="{ field, errors: fieldErrors }" name="selectAndAdd">
            <PfFormInputSelect
              :id="uniqueId"
              v-bind="field"
              class="w-full col-span-2"
              :label="props.selectLabel"
              has-hidden-label
              :options="props.options"
              :errors="fieldErrors"
              :placeholder="t('select-placeholder')" />
          </Field>
          <PfButtonAction class="w-full col-span-2" :label="t('save')" type="submit" />
        </div>
      </PfFormSection>
    </PfForm>
  </Form>
  <PfButtonAction v-else class="w-full" :label="props.addLabel" @click="() => emit('showSelect')" />
</template>

<script>
let instanceCounter = 0;
</script>

<script setup>
import { defineProps, defineEmits, computed } from "vue";
import { useI18n } from "vue-i18n";
import { object, string } from "yup";

const { t } = useI18n();

const props = defineProps({
  selectLabel: { type: String, required: true },
  addLabel: { type: String, required: true },
  options: {
    type: Array,
    default() {
      return [];
    }
  },
  showSelect: Boolean
});

const uniqueId = `ui-select-and-add-${++instanceCounter}`;

const validationSchema = computed(() =>
  object({
    selectAndAdd: string().label(props.selectLabel).required()
  })
);

const emit = defineEmits(["submit", "showSelect"]);

async function onSubmit({ selectAndAdd }) {
  emit("submit", selectAndAdd);
}
</script>

<i18n>
{
	"en": {
		"cancel-button-label": "Cancel",
		"delete-button-label": "Delete"
	},
	"fr": {
		"cancel-button-label": "Annuler",
		"delete-button-label": "Supprimer"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" ref="modal" :return-route="returnRoute" :title="title" :has-footer="false">
    <!-- eslint-disable-next-line vue/no-v-html -->
    <p v-html="description"></p>
    <Form v-slot="{ isSubmitting, errors: formErrors }" :validation-schema="validationSchema" @submit="deleteEntity">
      <PfForm ref="form" :disable-submit="Object.keys(formErrors).length > 0" :processing="isSubmitting">
        <Field v-slot="{ field, errors: fieldErrors }" name="deleteText">
          <PfFormInputText
            id="deleteText"
            v-bind="field"
            :label="props.deleteTextLabel"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <template #footer>
          <div class="pt-5">
            <div class="flex gap-x-6 items-center justify-end">
              <PfButtonAction btn-style="link" :label="props.cancelButtonLabel ?? defaultCancelLabel" @click="closeModal" />
              <PfButtonAction class="px-8" :label="props.deleteButtonLabel ?? defaultDeleteLabel" type="submit" />
            </div>
          </div>
        </template>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import { defineProps, defineEmits, defineExpose, ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { object, string } from "yup";

const { t } = useI18n();
const defaultDeleteLabel = t("delete-button-label");
const defaultCancelLabel = t("cancel-button-label");

const props = defineProps({
  title: { type: String, default: null },
  description: { type: String, default: null },
  validationText: { type: String, default: null },
  deleteTextLabel: { type: String, default: null },
  deleteTextError: { type: String, default: null },
  deleteButtonLabel: { type: String, default: null },
  cancelButtonLabel: { type: String, default: null },
  returnRoute: { type: Object, default: null }
});

const validationSchema = computed(() =>
  object({
    deleteText: string()
      .label(props.deleteTextLabel)
      .test({
        name: "sameValue",
        exclusive: false,
        params: {},
        message: props.deleteTextError,
        test: function (value) {
          if (props.validationText) return value === props.validationText;
          return false;
        }
      })
  })
);

const modal = ref(null);
const form = ref(null);

defineExpose({
  openModal: () => modal.value.openModal()
});

const emit = defineEmits(["onDelete"]);

const deleteEntity = () => {
  emit("onDelete");
};
</script>

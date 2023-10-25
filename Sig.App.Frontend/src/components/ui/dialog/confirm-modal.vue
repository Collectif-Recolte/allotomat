<i18n>
{
	"en": {
		"cancel-button-label": "Cancel",
		"confirm-button-label": "Confirm"
	},
	"fr": {
		"cancel-button-label": "Annuler",
		"confirm-button-label": "Confirmer"
	}
}
</i18n>

<template>
  <UiDialogModal
    class="text-center"
    :title="props.title"
    :return-route="props.returnRoute"
    :description="props.description"
    has-text-center
    is-overflowing>
    <template #body>
      <UiIconComplete />
      <DialogTitle as="h1" class="text-h2 font-semibold mt-0 mb-6 py-8 border-b border-primary-200">
        <slot name="title">
          {{ props.title }}
        </slot>
      </DialogTitle>
      <p class="text-p2 max-w-md mx-auto">{{ description }}</p>
    </template>
    <template #footer="{ closeModal }">
      <div class="flex flex-col items-center justify-center gap-y-6 mt-6 mb-4">
        <PfButtonLink
          v-if="props.confirmRoute"
          tag="RouterLink"
          :label="props.confirmButtonLabel ?? defaultConfirmLabel"
          :to="confirmRoute" />
        <PfButtonAction v-else :label="props.confirmButtonLabel ?? defaultConfirmLabel" @click="confirm" />
        <PfButtonAction
          v-if="props.returnRoute"
          btn-style="link"
          :label="props.cancelButtonLabel ?? defaultCancelLabel"
          @click="closeModal" />
      </div>
    </template>
  </UiDialogModal>
</template>

<script setup>
import { defineProps, defineEmits, defineExpose, ref } from "vue";
import { useI18n } from "vue-i18n";
import { DialogTitle } from "@headlessui/vue";

const { t } = useI18n();
const defaultConfirmLabel = t("confirm-button-label");
const defaultCancelLabel = t("cancel-button-label");

const props = defineProps({
  title: { type: String, default: null },
  description: { type: String, default: null },
  confirmButtonLabel: { type: String, default: null },
  cancelButtonLabel: { type: String, default: null },
  icon: { type: Object, default: null },
  returnRoute: { type: Object, default: null },
  confirmRoute: { type: Object, default: null }
});

const modal = ref(null);
defineExpose({
  openModal: () => modal.value.openModal(),
  isOpen: () => modal.value.isOpen()
});

// Événements
const emit = defineEmits(["confirm"]);
const confirm = () => {
  emit("confirm");
};
</script>

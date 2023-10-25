<i18n>
{
	"en": {
		"cancel-button-label": "Cancel",
		"confirm-button-label": "I understand, continue"
	},
	"fr": {
		"cancel-button-label": "Annuler",
		"confirm-button-label": "J'ai compris, continuer"
	}
}
</i18n>

<template>
  <UiDialogModal :title="props.title" :return-route="props.returnRoute" :description="props.description" is-overflowing>
    <template #body>
      <DialogTitle as="h1" class="text-h2 font-semibold mt-0 mb-8 xs:mb-12">
        <slot name="title">
          {{ props.title }}
        </slot>
      </DialogTitle>
      <div class="flex flex-col xs:flex-row gap-4 xs:gap-8 xs:items-center">
        <img class="w-32 xs:w-80 h-auto" :src="require('@/assets/img/warning.svg')" alt="" />
        <slot name="description">
          <p class="text-p1 font-bold text-primary-500">{{ props.description }}</p>
        </slot>
      </div>
    </template>
    <template #footer="{ closeModal }">
      <div class="flex items-center gap-x-6 flex-shrink-0 justify-end ml-auto mr-0 border-grey-300 border-t pt-4 mt-8 xs:mt-12">
        <PfButtonAction
          v-if="props.returnRoute"
          btn-style="link"
          :label="props.cancelButtonLabel ?? defaultCancelLabel"
          @click="closeModal" />
        <PfButtonAction v-else btn-style="link" :label="props.cancelButtonLabel ?? defaultCancelLabel" @click="goBack" />
        <PfButtonLink
          v-if="props.confirmRoute"
          tag="RouterLink"
          :label="props.confirmButtonLabel ?? defaultConfirmLabel"
          :to="confirmRoute" />
        <PfButtonAction v-else :label="props.confirmButtonLabel ?? defaultConfirmLabel" @click="confirm" />
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
const emit = defineEmits(["confirm", "goBack"]);
const confirm = () => {
  emit("confirm");
};
const goBack = () => {
  emit("goBack");
};
</script>

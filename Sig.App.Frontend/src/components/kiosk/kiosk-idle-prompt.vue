<i18n>
{
  "en": {
    "still-there-title": "Are you still there?",
    "still-there-action": "Yes, I'm still here",
    "returning-soon": "Returning to the main menu in {seconds} s"
  },
  "fr": {
    "still-there-title": "Êtes-vous toujours là?",
    "still-there-action": "Oui, je suis là",
    "returning-soon": "Retour au menu principal dans {seconds} s"
  }
}
</i18n>

<template>
  <UiDialogModal class="text-center" :title="t('still-there-title')" has-text-center hide-main-btn @onClose="emit('dismiss')">
    <template #body>
      <DialogTitle as="h2" class="text-d2 font-bold text-primary-700 mt-12 mb-4">
        {{ t("still-there-title") }}
      </DialogTitle>
      <p class="text-h4 text-grey-600 mb-0">
        {{ t("returning-soon", { seconds: warningSecondsRemaining }) }}
      </p>
    </template>
    <template #footer>
      <div class="flex justify-center pt-6 pb-12">
        <PfButtonAction
          size="lg"
          class="min-h-20 rounded-2xl text-d6"
          btn-style="primary"
          :label="t('still-there-action')"
          @click="emit('dismiss')" />
      </div>
    </template>
  </UiDialogModal>
</template>

<script setup>
import { defineEmits, defineProps } from "vue";
import { useI18n } from "vue-i18n";
import { DialogTitle } from "@headlessui/vue";

defineProps({
  warningSecondsRemaining: { type: Number, required: true }
});

const emit = defineEmits(["dismiss"]);
const { t } = useI18n();
</script>

<i18n>
{
  "en": {
    "still-there-title": "Are you still there?",
    "still-there-action": "Yes, I'm still here",
    "quit-now-action": "Quit now",
    "returning-soon": "Returning to the main menu in {seconds} s"
  },
  "fr": {
    "still-there-title": "Êtes-vous toujours là?",
    "still-there-action": "Oui, je suis là",
    "quit-now-action": "Quitter maintenant",
    "returning-soon": "Retour au menu principal dans {seconds} s"
  }
}
</i18n>

<template>
  <UiDialogModal class="text-center" has-text-center hide-main-btn @onClose="emit('dismiss')">
    <template #body>
      <DialogTitle as="h2" class="text-d2 font-bold text-primary-700 mt-12 mb-4">
        {{ t("still-there-title") }}
      </DialogTitle>
      <p class="text-h4 text-grey-600 mb-0">
        {{ t("returning-soon", { seconds: warningSecondsRemaining }) }}
      </p>
    </template>
    <template #footer>
      <div class="flex flex-col sm:flex-row justify-center gap-4 pt-6 pb-12 px-6">
        <PfButtonAction
          size="lg"
          class="min-h-20 rounded-2xl text-d6"
          btn-style="primary"
          :label="t('still-there-action')"
          @click="emit('dismiss')" />
        <PfButtonAction
          size="lg"
          class="min-h-20 rounded-2xl text-d6"
          btn-style="secondary"
          :label="t('quit-now-action')"
          @click="emit('quit')" />
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

const emit = defineEmits(["dismiss", "quit"]);
const { t } = useI18n();
</script>

<i18n>
  {
    "en": {
      "close": "Close"
    },
    "fr": {
      "close": "Fermer"
    }
  }
</i18n>

<template>
  <TransitionRoot as="template" :show="isOpen">
    <Dialog as="div" class="fixed z-40 inset-0 overflow-y-auto" @close="closeModal">
      <div class="flex items-center justify-center min-h-screen py-4 px-4 text-center sm:block sm:p-0">
        <TransitionChild as="template" v-bind="overlayTransition">
          <DialogOverlay class="transition-opacity fixed inset-0 bg-primary-700/80" />
        </TransitionChild>

        <!-- This element is to trick the browser into centering the modal contents. -->
        <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
        <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>
        <TransitionChild as="template" v-bind="modalTransition">
          <div
            class="inline-block relative align-bottom bg-white dark:bg-grey-800 rounded-2xl px-4 pt-5 pb-4 shadow-xl transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full sm:p-6"
            :class="[{ 'text-left': !hasTextCenter }, { 'overflow-hidden': !isOverflowing }]">
            <div>
              <slot name="body">
                <div v-if="props.icon" class="mx-auto flex items-center justify-center text-grey-500 mb-3 sm:mb-5">
                  <PfIcon size="2xl" fill-class="" stroke-class="stroke-current" :icon="props.icon" aria-hidden="true" />
                </div>
                <DialogTitle as="h2" class="text-2xl leading-6 font-semibold text-gray-900 mt-0 mb-6">
                  <slot name="title" :closeModal="closeModal">
                    {{ props.title }}
                  </slot>
                </DialogTitle>
                <div class="mt-2">
                  <slot :closeModal="closeModal">
                    <p class="text-sm text-gray-500">{{ props.description }}</p>
                  </slot>
                </div>
              </slot>
            </div>
            <slot name="footer" :closeModal="closeModal">
              <div v-if="hasFooter" class="pt-5 flex gap-x-6 gap-y-3 flex-wrap xs:flex-nowrap">
                <PfInfo v-if="warningMessage" :message="warningMessage" />
                <div class="flex items-center gap-x-6 flex-shrink-0 justify-end ml-auto mr-0">
                  <slot name="actions" :closeModal="closeModal"></slot>
                  <PfButtonAction v-if="!hideMainBtn" class="px-8" :label="closeLabel || t('close')" @click="closeModal" />
                </div>
              </div>
            </slot>
          </div>
        </TransitionChild>
      </div>
    </Dialog>
  </TransitionRoot>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { ref, defineProps, defineExpose, defineEmits, onMounted } from "vue";
import { Dialog, DialogOverlay, DialogTitle, TransitionChild, TransitionRoot } from "@headlessui/vue";
import { useRouter } from "vue-router";

const router = useRouter();
const { t } = useI18n();
const emit = defineEmits(["onClose"]);

const props = defineProps({
  title: { type: String, default: null },
  description: { type: String, default: null },
  icon: { type: Object, default: null },
  hasFooter: { type: Boolean, default: true },
  closeLabel: { type: String, default: "" },
  returnRoute: { type: Object, default: null },
  warningMessage: { type: String, default: "" },
  manualModalOpening: Boolean,
  hasTextCenter: Boolean,
  isOverflowing: Boolean,
  hideMainBtn: Boolean
});

// Ouverture / fermeture
const isOpen = ref(false);

const openModal = () => {
  isOpen.value = true;
};

const closeModal = () => {
  isOpen.value = false;
  if (props.returnRoute) {
    router.push(props.returnRoute);
  } else {
    emit("onClose");
  }
};

onMounted(() => {
  if (!props.manualModalOpening) {
    openModal();
  }
});

// Méthodes et props accessible par le component parent
defineExpose({ closeModal, openModal, isOpen });

// Style de transitions
const overlayTransition = {
  enter: "ease-out duration-300",
  enterFrom: "opacity-0",
  enterTo: "opacity-100",
  leave: "ease-in duration-200",
  leaveFrom: "opacity-100",
  leaveTo: "opacity-0"
};

const modalTransition = {
  enter: "ease-out duration-300",
  enterFrom: "opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95",
  enterTo: "opacity-100 translate-y-0 sm:scale-100",
  leave: "ease-in duration-200",
  leaveFrom: "opacity-100 translate-y-0 sm:scale-100",
  leaveTo: "opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
};
</script>

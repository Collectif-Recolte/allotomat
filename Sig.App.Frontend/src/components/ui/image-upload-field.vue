<i18n>
  {
    "en": {
      "image-prompt": "Drop or choose an image",
      "delete-image": "Delete image",
    },
    "fr": {
      "image-prompt": "Déposer ou choisir une image",
      "delete-image": "Supprimer l'image",
    }
  }
  </i18n>

<template #default="{ handleChange, field }">
  <div>
    <label class="block text-sm font-medium text-grey-700 dark:text-grey-300 mb-2">{{ props.label }}</label>

    <div v-if="field.value" class="max-w-[544px]">
      <div class="relative w-full h-0 pb-[calc(355/544*100%)] max-w-full">
        <div class="absolute inset-0 bg-grey-50">
          <img class="absolute object-contain" :src="getImageUrl(field.value)" />
          <slot name="guidelines"></slot>
        </div>
        <UiButtonGroup
          class="absolute top-8 right-8"
          :items="getBtnGroup(handleChange)"
          tooltip-position="left"
          :btn-custom-classes="['bg-white']" />
      </div>
    </div>

    <UiSingleFileUpload v-else accept="image/*" target="images" @fileUploaded="(context) => handleChange(context.fileId)">
      <p class="text-grey-700 font-semibold mb-4">{{ t("image-prompt") }}</p>
    </UiSingleFileUpload>

    <PfInfo v-if="warningMessage" :message="warningMessage" class="mt-4 max-w-sm" />
  </div>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import ICON_TRASH from "@/lib/icons/trash.json";

const { t } = useI18n();

const props = defineProps({
  label: {
    type: String,
    default: ""
  },
  descriptionUpload: {
    type: String,
    default: null
  },
  descriptionPreview: {
    type: String,
    default: null
  },
  field: {
    type: Object,
    required: true
  },
  handleChange: {
    type: Function,
    required: true
  }
});

const warningMessage = computed(() => (props.field.value ? props.descriptionPreview : props.descriptionUpload));

function getImageUrl(imageId) {
  if (!imageId) return null;
  return `/images/${imageId}?width=544&height=355&rmode=max`;
}

const getBtnGroup = (handleChange) => [
  {
    icon: ICON_TRASH,
    label: t("delete-image"),
    onClick: () => handleChange("")
  }
];
</script>

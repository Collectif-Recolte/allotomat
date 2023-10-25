<i18n lang="json">
{
  "fr": {
    "choose-file": "Choisir un fichier",
    "default-title": "Déposez ou choisissez un fichier",
    "drop-text": "Déposez le fichier pour téléverser",
    "loading": "Traitement en cours..."
  },
  "en": {
    "choose-file": "Choose file",
    "default-title": "Drop or choose a file",
    "drop-text": "Drop file to upload",
    "loading": "Processing..."
  }
}
</i18n>

<template>
  <!-- Attention: le v-if est important, voir note dans le code -->
  <FileSelector v-if="files.length === 0" v-model="files" :accept="accept" :allow-multiple="false">
    <Dropzone v-slot="{ hovered }">
      <div class="relative">
        <div :class="loading ? 'opacity-100' : 'opacity-0'" class="overlay">
          <slot name="loading">{{ t("loading") }}</slot>
        </div>
        <div :class="hovered && !loading ? 'opacity-100' : 'opacity-0'" class="overlay">
          <slot name="drop-overlay">{{ t("drop-text") }}</slot>
        </div>

        <div class="border-2 border-grey-300 border-dashed rounded-lg h-remove-margin px-4 xs:px-6 md:px-8 py-10 text-center">
          <slot>
            <p class="text-xl leading-7 font-semibold mb-1">{{ t("default-title") }}</p>
            <p class="text-sm leading-5 text-grey-500 mb-6">{{ description }}</p>
          </slot>

          <slot name="button">
            <DialogButton type="button" class="pf-button pf-button--outline pf-button--sm">
              {{ t("choose-file") }}
            </DialogButton>
          </slot>
        </div>
      </div>
    </Dropzone>
  </FileSelector>
</template>

<script setup>
import { computed, defineEmits, defineProps, nextTick, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { FileSelector, Dropzone, DialogButton } from "vue3-file-selector";

import { uploadTempFile, uploadImage } from "@/lib/services/upload";

const { t } = useI18n();

const files = ref([]);
const loading = ref(false);

const emit = defineEmits(["fileUploaded"]);

const props = defineProps({
  accept: {
    type: [String, Array],
    default: null
  },
  target: {
    type: String,
    required: true,
    validator(target) {
      return ["temp", "images", "none"].includes(target);
    }
  },
  description: {
    type: String,
    default: null
  }
});

const accept = computed(() => props.accept && (typeof props.accept === "string" ? [props.accept] : props.accept));

watch(files, async ([file]) => {
  if (!file) return;

  // Note: cette ligne, combinée avec le v-if sur le FileSelector, est un semi-hack pour forcer le reset du composant entre chaque upload.
  // Si on ne fait pas ça, le composant ne permet pas d'uploader deux fois de suite le même fichier.
  nextTick(() => {
    files.value = [];
  });

  if (props.target === "temp") {
    loading.value = true;

    try {
      const fileId = await uploadTempFile(file);
      let handlePromise = null;

      emit("fileUploaded", {
        fileId,
        handle: (asyncAction) => (handlePromise = asyncAction())
      });

      await handlePromise;
    } finally {
      loading.value = false;
    }
  } else if (props.target === "images") {
    loading.value = true;

    try {
      const fileId = await uploadImage(file);
      let handlePromise = null;

      emit("fileUploaded", {
        fileId,
        handle: (asyncAction) => (handlePromise = asyncAction())
      });

      await handlePromise;
    } finally {
      loading.value = false;
    }
  } else if (props.target === "none") {
    loading.value = true;

    try {
      let handlePromise = null;

      emit("fileUploaded", {
        handle: (asyncAction) => (handlePromise = asyncAction(file))
      });

      await handlePromise;
    } finally {
      loading.value = false;
    }
  }
});
</script>

<style lang="postcss" scoped>
.overlay {
  @apply bg-grey-700 rounded-md text-white text-xl font-semibold 
          absolute h-full w-full z-10 
          flex items-center justify-center 
          pointer-events-none transition-opacity;
}
</style>

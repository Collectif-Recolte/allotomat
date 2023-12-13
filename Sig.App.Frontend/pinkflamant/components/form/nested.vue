<template>
  <div
    class="pf-form-nested rounded-lg p-4 pt-3 flex flex-col gap-y-3 border transition-colors duration-300 ease-in-out"
    :class="isDisabled ? 'border-primary-300' : 'border-transparent bg-secondary-100'">
    <!-- Form body -->
    <slot></slot>

    <!-- Form footer -->
    <div class="flex gap-6 flex-col xs:flex-row xs:justify-between xs:items-end">
      <slot name="lastField"></slot>
      <div class="flex flex-shrink-0 gap-x-6 items-center justify-end">
        <slot
          name="footer"
          :processing="processing"
          :disableSubmit="disableSubmit"
          :loadingLabel="loadingLabel"
          :submitLabel="submitLabel">
          <PfButtonAction
            v-if="canCancel"
            btn-type="button"
            btn-style="link"
            size="sm"
            :label="cancelLabel"
            :is-disabled="processing"
            @click="$emit('cancel')" />
          <div class="relative flex items-center">
            <PfButtonAction
              btn-type="submit"
              class="px-8"
              size="sm"
              :is-disabled="disableSubmit || processing || isFormDisabled"
              :label="submitLabel" />
            <div class="absolute -translate-y-1/2 top-1/2 right-1">
              <PfSpinner v-if="processing" text-color-class="text-white" :loading-label="loadingLabel" is-small />
            </div>
          </div>
        </slot>
      </div>
    </div>
  </div>
</template>

<script>
import { useIsFormDirty, useIsFormValid } from "vee-validate";

export default {
  props: {
    submitLabel: {
      type: String,
      default: "Sauvegarder"
    },
    cancelLabel: {
      type: String,
      default: "Annuler"
    },
    loadingLabel: {
      type: String,
      default: "En chargement..."
    },
    canCancel: Boolean,
    disableSubmit: Boolean,
    processing: Boolean,
    isDisabled: Boolean
  },
  emits: ["cancel"],
  computed: {
    isFormDisabled() {
      const isDirty = useIsFormDirty();
      const isValid = useIsFormValid();
      return !isDirty.value || !isValid.value;
    }
  }
};
</script>

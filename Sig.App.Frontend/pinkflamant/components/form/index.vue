<template>
  <div class="pf-form space-y-8 divide-y divide-grey-300">
    <!-- Form body -->
    <div class="space-y-8 divide-y divide-grey-300">
      <slot></slot>
    </div>

    <!-- Form footer -->
    <slot name="footer">
      <div v-if="hasFooter" class="pt-5 flex items-center flex-wrap gap-x-6 gap-y-3 xs:flex-nowrap">
        <PfInfo v-if="hasWarning" :message="warningMessage">
          <slot name="warning"></slot>
        </PfInfo>
        <div
          class="flex items-center flex-shrink-0 gap-x-6"
          :class="[hasWarning ? 'ml-auto mr-0' : 'w-full', footerAltStyle ? 'justify-between' : 'justify-end']">
          <PfButtonLink
            v-if="cancelRoute"
            tag="RouterLink"
            btn-style="link"
            :label="cancelLabel"
            :is-disabled="processing"
            :to="cancelRoute" />
          <PfButtonAction
            v-else-if="canCancel"
            btn-type="button"
            btn-style="link"
            :label="cancelLabel"
            :is-disabled="processing"
            @click="$emit('cancel')" />
          <div class="relative flex items-center">
            <PfTooltip class="group-pfone" :hide-tooltip="!submitTooltipLabel || hideTooltip" :label="submitTooltipLabel">
              <PfButtonAction
                class="px-8"
                :class="{ 'text-h4': footerAltStyle }"
                :btn-style="footerAltStyle ? 'secondary' : 'primary'"
                btn-type="submit"
                :is-disabled="disableSubmit || processing || isDisabled"
                :label="submitLabel" />
              <div class="absolute -translate-y-1/2 top-1/2 right-1">
                <PfSpinner v-if="processing" text-color-class="text-white" :loading-label="loadingLabel" is-small />
              </div>
            </PfTooltip>
          </div>
        </div>
      </div>
    </slot>
  </div>
</template>

<script>
import { useIsFormDirty, useIsFormValid, useIsFormTouched } from "vee-validate";

export default {
  props: {
    hasFooter: Boolean,
    submitLabel: {
      type: String,
      default: "Sauvegarder"
    },
    submitTooltipLabel: {
      type: String,
      default: ""
    },
    cancelLabel: {
      type: String,
      default: "Annuler"
    },
    loadingLabel: {
      type: String,
      default: "En chargement..."
    },
    warningMessage: {
      type: String,
      default: ""
    },
    cancelRoute: {
      type: Object,
      default: null
    },
    canCancel: Boolean,
    disableSubmit: Boolean,
    processing: Boolean,
    footerAltStyle: Boolean,
    hideTooltip: Boolean
  },
  emits: ["cancel"],
  computed: {
    hasWarning() {
      return this.warningMessage || this.$slots.warning;
    },
    isDisabled() {
      const isDirty = useIsFormDirty();
      const isTouched = useIsFormTouched();
      const isValid = useIsFormValid();

      console.log("isDirty::" + isDirty.value + " isTouched::" + isTouched.value + " isValid::" + isValid.value);

      return (!isDirty.value && !isTouched.value && !isValid.value) || !isValid.value;
    }
  }
};
</script>

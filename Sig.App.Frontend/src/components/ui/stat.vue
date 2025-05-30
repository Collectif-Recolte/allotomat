<template>
  <div class="border-primary-300 border rounded-2xl p-5 h-remove-margin flex flex-col sm:min-h-52">
    <p v-if="label" class="text-d7 mb-2 font-bold">{{ props.label }}</p>
    <span class="text-d2 xl:text-d1 font-bold text-primary-900">{{ props.stat }}</span>
    <div
      class="mt-4 xs:mt-auto mb-0 flex items-end space-y-2 flex-col sm:space-y-0 sm:flex-row sm:justify-between sm:items-center sm:min-h-10">
      <PfButtonLink
        v-if="primaryBtnRoute"
        btn-style="link"
        tag="routerLink"
        :to="primaryBtnRoute"
        :label="primaryBtnLabel"
        size="lg" />
      <PfButtonAction
        v-else-if="primaryBtnIsAction"
        btn-style="link"
        :label="primaryBtnLabel"
        size="lg"
        @click="() => emit('onPrimaryBtnClick')" />
      <PfButtonLink
        v-if="secondaryBtnRoute"
        class="sm:ml-auto"
        tag="routerLink"
        :to="secondaryBtnRoute"
        :label="secondaryBtnLabel"
        size="sm" />
      <PfButtonAction
        v-else-if="secondaryBtnIsAction"
        :label="secondaryBtnLabel"
        class="sm:ml-auto"
        size="sm"
        @click="() => emit('onSecondaryBtnClick')" />
    </div>
  </div>
</template>

<script setup>
import { defineProps, defineEmits } from "vue";

const props = defineProps({
  label: { type: String, default: "" },
  stat: { type: [Number, String], default: 0 },
  primaryBtnLabel: { type: String, default: "" },
  primaryBtnRoute: { type: Object, default: null },
  primaryBtnIsAction: Boolean,
  secondaryBtnLabel: { type: String, default: "" },
  secondaryBtnRoute: { type: Object, default: null },
  secondaryBtnIsAction: Boolean
});

const emit = defineEmits(["onPrimaryBtnClick"]);
</script>

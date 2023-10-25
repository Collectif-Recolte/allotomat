<template>
  <div class="bg-white rounded-2xl shadow-2xl inline-flex flex-col items-center justify-center text-center p-6 h-remove-margin">
    <img class="w-72 h-auto max-w-full mb-6" :src="props.imgSrc" alt="" aria-hidden />
    <p v-if="description" class="text-p3 mb-4">{{ props.description }}</p>
    <PfButtonLink
      v-if="primaryBtnRoute"
      class="w-full mb-6"
      btn-style="secondary"
      tag="routerLink"
      :to="primaryBtnRoute"
      :label="primaryBtnLabel"
      :icon="primaryBtnIcon"
      has-icon-left
      size="lg" />
    <PfButtonAction
      v-else-if="primaryBtnIsAction"
      class="w-full mb-6"
      btn-style="secondary"
      :label="primaryBtnLabel"
      :icon="primaryBtnIcon"
      has-icon-left
      size="lg"
      @click="() => emit('onPrimaryBtnClick')" />
    <slot name="secondaryBtn">
      <PfButtonLink
        v-if="secondaryBtnRoute"
        btn-style="link"
        tag="routerLink"
        :to="secondaryBtnRoute"
        :label="secondaryBtnLabel" />
    </slot>
  </div>
</template>

<script setup>
import { defineProps, defineEmits } from "vue";

const props = defineProps({
  description: { type: String, default: "" },
  imgSrc: { type: String, default: require("@/assets/img/default.jpg") },

  primaryBtnLabel: { type: String, default: "" },
  primaryBtnIcon: { type: Object, default: null },
  primaryBtnRoute: { type: Object, default: null },
  primaryBtnIsAction: Boolean,

  secondaryBtnLabel: { type: String, default: "" },
  secondaryBtnRoute: { type: Object, default: null }
});

const emit = defineEmits(["onPrimaryBtnClick"]);
</script>

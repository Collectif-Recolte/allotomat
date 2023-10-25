<template>
  <div class="flex flex-col gap-y-4 sm:flex-row sm:gap-x-4 sm:justify-between sm:items-center pb-5">
    <div>
      <h2 v-if="props.title" class="my-0">{{ props.title }}</h2>
    </div>

    <div>
      <slot name="right">
        <PfButtonLink v-if="props.ctaRoute" tag="RouterLink" :to="props.ctaRoute" :label="props.ctaLabel" />
      </slot>
    </div>
  </div>
</template>

<script setup>
import { defineProps } from "vue";

const props = defineProps({
  title: {
    type: String,
    default: ""
  },
  ctaLabel: {
    type: String,
    default: ""
  },
  ctaRoute: {
    type: Object,
    default: null
  }
});
</script>

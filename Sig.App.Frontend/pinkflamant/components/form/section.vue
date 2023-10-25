<template>
  <div class="pf-form-section pt-6 first:pt-0">
    <!-- Section head -->
    <slot name="header">
      <div v-if="title || description" class="mb-6">
        <h3 v-if="title" class="text-lg leading-6 font-semibold text-grey-900 my-0" :aria-describedby="uniqueId">
          {{ title }}
        </h3>
        <p v-if="description" :id="uniqueId" class="mt-1 text-sm text-grey-500">
          {{ description }}
        </p>
      </div>
    </slot>
    <!-- Section body -->
    <div class="grid grid-cols-1 gap-y-6 gap-x-4" :class="{ 'sm:grid-cols-12': isGrid }">
      <slot></slot>
    </div>
  </div>
</template>

<script>
let instanceCounter = 0;
export default {
  props: {
    isGrid: Boolean,
    title: {
      type: String,
      default: ""
    },
    description: {
      type: String,
      default: ""
    }
  },
  data() {
    return {
      uniqueId: `pf-section-desc-${++instanceCounter}`
    };
  }
};
</script>

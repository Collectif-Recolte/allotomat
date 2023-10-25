<template>
  <picture>
    <template v-if="breakpoints.length > 0">
      <source
        v-for="(breakpoint, index) in breakpoints"
        :key="index"
        :media="breakpoint.screen ? `(min-width: ${breakpoint.screen})` : null"
        :srcset="`${breakpoint.src}, ${breakpoint.srcRetina} 2x`" />
    </template>
    <img class="w-full h-full absolute inset-0" :class="isContained ? 'object-contain' : 'object-cover'" :src="src" :alt="alt" />
  </picture>
</template>

<script>
export default {
  props: {
    src: {
      type: String,
      default: ""
    },
    alt: {
      type: String,
      default: ""
    },
    breakpoints: {
      type: Array,
      default() {
        return [];
      }
    },
    isContained: Boolean
  }
};
</script>

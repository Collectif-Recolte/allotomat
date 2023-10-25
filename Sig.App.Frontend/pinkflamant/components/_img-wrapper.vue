<template>
  <div class="pf-img-wrapper h-full" :class="[{ 'absolute inset-0': isAbsolutePos }]">
    <div
      class="relative overflow-hidden"
      :class="[{ 'h-0': heightClass.startsWith('p') }, heightClass, widthClass, { 'rounded-full': isCircle }]">
      <Picture :src="src" :alt="alt" :breakpoints="breakpoints" />
    </div>
  </div>
</template>

<script>
import Picture from "./_picture";

export default {
  components: {
    Picture
  },
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
    heightClass: {
      type: String,
      default: "h-64"
    },
    widthClass: {
      type: String,
      default: "w-full"
    },
    isContained: Boolean,
    isAbsolutePos: Boolean,
    isCircle: Boolean
  }
};
</script>

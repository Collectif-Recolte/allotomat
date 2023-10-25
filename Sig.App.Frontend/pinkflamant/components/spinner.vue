<template>
  <div
    class="pf-spinner relative motion-reduce:h-auto motion-reduce:w-auto"
    :class="[textColorClass, isSmall ? 'h-6 w-6' : 'h-12 w-12']">
    <span class="sr-only motion-reduce:not-sr-only">{{ loadingLabel }}</span>
    <svg
      class="pf-animation-spinner fill-current motion-reduce:hidden"
      :class="{ 'pf-animation-spinner--small': isSmall }"
      viewBox="0 0 50 50">
      <circle
        class="pf-animation-spinner__path stroke-current"
        stroke-linecap="round"
        cx="25"
        cy="25"
        r="20"
        fill="none"
        stroke-width="5"></circle>
    </svg>
  </div>
</template>

<script>
export default {
  props: {
    loadingLabel: {
      type: String,
      default: "En chargement..."
    },
    textColorClass: {
      type: String,
      default: "text-primary-700"
    },
    isSmall: Boolean
  }
};
</script>

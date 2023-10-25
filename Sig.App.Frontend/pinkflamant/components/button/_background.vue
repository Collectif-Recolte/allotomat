<template>
  <div
    v-if="transition"
    :class="[
      `pf-transition-${transition.name}__bg pf-transition-${transition.name}__bg--${transition.color}`,
      transition.hasAlternateBgColor ? `pf-transition-${transition.name}__bg--bg-secondary` : ''
    ]"></div>
</template>

<script>
export default {
  props: {
    transition: {
      type: Object,
      default: null
    }
  }
};
</script>

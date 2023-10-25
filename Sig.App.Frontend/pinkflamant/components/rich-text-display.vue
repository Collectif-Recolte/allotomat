<template>
  <div class="pf-text" :class="{ 'h-remove-margin': removeMargin }">
    <slot>
      <!-- eslint-disable-next-line vue/no-v-html -->
      <div :class="{ 'h-remove-margin': removeMargin }" v-html="htmlContent"></div>
    </slot>
  </div>
</template>

<script>
export default {
  props: {
    htmlContent: {
      type: String,
      default: ""
    },
    removeMargin: Boolean
  }
};
</script>

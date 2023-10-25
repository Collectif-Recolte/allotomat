<template>
  <div class="pf-block px-section max-w-full mx-auto" :class="[verticalMarginClass, widthClass]">
    <slot></slot>
  </div>
</template>

<script>
export default {
  props: {
    verticalMarginClass: {
      type: String,
      default: "my-16 md:my-24"
    },
    widthClass: {
      type: String,
      default: "w-xl"
    }
  }
};
</script>

<template>
  <hr class="pf-separator border-0 h-0" :class="[paddingTopClass, bgColorClass]" />
</template>

<script>
export default {
  props: {
    bgColorClass: {
      type: String,
      default: "bg-grey-500"
    },
    paddingTopClass: {
      type: String,
      default: "pt-1"
    }
  }
};
</script>

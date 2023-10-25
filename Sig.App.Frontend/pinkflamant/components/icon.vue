<template>
  <svg
    class="shrink-0 flex items-center justify-center"
    :class="[colorClass, sizeClass]"
    :width="`${icon.width}`"
    :height="`${icon.height}`"
    :viewBox="`0 0 ${icon.width} ${icon.height}`"
    fill="none"
    xmlns="http://www.w3.org/2000/svg">
    <path
      :stroke-linecap="icon.strokeLinecap || null"
      :stroke-linejoin="icon.strokeLinejoin || null"
      :stroke-width="icon.strokeWidth || null"
      :fill-rule="icon.fillRule || null"
      :clip-path="icon.clipPath || null"
      :d="icon.path" />
  </svg>
</template>

<script>
export default {
  props: {
    icon: {
      type: Object,
      required: true
    },
    size: {
      type: String,
      default: "md"
    },
    customSizeClass: {
      type: String,
      default: ""
    },
    color: {
      type: String,
      default: ""
    }
  },
  emits: [],
  computed: {
    sizeClass() {
      if (this.customSizeClass) {
        return this.customSizeClass;
      } else {
        switch (this.size) {
          case "xxs":
            return "w-2 h-2";
          case "xs":
            return "w-3 h-3";
          case "sm":
            return "w-4 h-4";
          case "md":
            return "w-5 h-5";
          case "lg":
            return "w-6 h-6";
          case "xl":
            return "w-8 h-8";
          case "2xl":
            return "w-10 h-10";
          default:
            return "w-5 h-5";
        }
      }
    },
    colorClass() {
      if (this.icon.strokeWidth > 0) {
        if (this.color) return `stroke-${this.color}`;
        return "stroke-current";
      } else {
        if (this.color) return `fill-${this.color}`;
        return "fill-current";
      }
    }
  }
};
</script>

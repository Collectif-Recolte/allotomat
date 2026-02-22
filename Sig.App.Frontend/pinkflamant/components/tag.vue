<template>
  <span
    v-if="showTag"
    class="pf-tag inline-flex text-p2"
    :class="[
      bgColorClass,
      borderColorClass,
      { border: borderColorClass },
      { dark: isDarkTheme },
      allowWrap ? 'w-fit max-w-48 flex-none self-start items-start gap-x-2 py-1.5 px-2.5 rounded-lg' : 'max-w-full items-center py-1',
      !allowWrap && (isSquared ? 'rounded-md px-1.5 py-0.5' : 'rounded-full px-2.5 py-1')
    ]">
    <span
      class="block dark:text-white"
      :class="[
        { 'text-p4 font-bold': isSquared },
        allowWrap ? 'min-w-0 break-words whitespace-normal leading-snug py-0.5' : 'truncate'
      ]"
      >{{ label }}</span
    >
    <button
      v-if="canDismiss"
      type="button"
      class="flex-shrink-0 mt-0.5 h-[16px] w-[16px] rounded-full inline-flex items-center justify-center bg-white text-primary-700 hover:bg-yellow-500 focus:outline-none ring-offset-2 focus-visible:ring-2 focus-visible:ring-yellow-500 transition-colors duration-200 ease-in-out"
      :class="allowWrap ? '-mr-0.5' : 'ml-2.5 -mr-1'"
      @click="dismiss()">
      <span class="sr-only">{{ dismissLabel }}</span>
      <Icon :icon="ICON_CLOSE" custom-size-class="w-[8px] h-[8px]" aria-hidden="true" />
    </button>
  </span>
</template>

<script>
import ICON_CLOSE from "../icons/close.json";
import Icon from "./icon";

export default {
  components: {
    Icon
  },
  props: {
    bgColorClass: {
      type: String,
      default: "bg-white"
    },
    borderColorClass: {
      type: String,
      default: ""
    },
    label: {
      type: String,
      default: ""
    },
    dismissLabel: {
      type: String,
      default: ""
    },
    isDarkTheme: Boolean,
    canDismiss: Boolean,
    isSquared: Boolean,
    hideTagOnDismiss: Boolean,
    allowWrap: {
      type: Boolean,
      default: false
    }
  },
  emits: ["dismiss"],
  data() {
    return {
      ICON_CLOSE,
      showTag: true
    };
  },
  methods: {
    dismiss() {
      this.$emit("dismiss");
      if (this.hideTagOnDismiss) {
        this.showTag = false;
      }
    }
  }
};
</script>

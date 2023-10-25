<template>
  <div class="relative flex items-center pf-transition-visibility">
    <slot :tooltipId="uniqueId"></slot>
    <span
      v-if="!hideTooltip"
      :id="uniqueId"
      ref="tooltip"
      class="pf-tooltip pf-transition-visibility__content absolute flex items-center z-20 py-2 px-3 text-p4 whitespace-nowrap bg-primary-900 dark:bg-grey-700 text-white leading-tight font-normal shadow-md rounded-full focus-within:opacity-100 before:h-2 before:w-2 before:rotate-45 before:bg-primary-900 before:border-grey-700 dark:before:bg-grey-700 dark:before:border-grey-600 before:absolute"
      :class="{
        '-top-2 -translate-x-1/2 -translate-y-full left-1/2 before:bottom-0 before:left-1/2 before:translate-y-1/2 before:-translate-x-1/2 before:border-r before:border-b':
          position === 'top',
        '-bottom-2 -translate-x-1/2 translate-y-full left-1/2 before:top-0 before:left-1/2 before:-translate-y-1/2 before:-translate-x-1/2 before:border-t before:border-l':
          position === 'bottom',
        '-top-2 -translate-y-full right-0 before:bottom-0 before:right-3 before:translate-y-1/2 before:-translate-x-1/2 before:border-r before:border-b':
          position === 'left',
        '-top-2 -translate-y-full left-0 before:bottom-0 before:left-3.5 before:translate-y-1/2 before:border-r before:border-b':
          position === 'right'
      }">
      <span class="relative -top-px">{{ label }}</span>
      <button
        class="relative h-extend-cursor-area ml-2 opacity-100 transition-opacity ease-in-out duration-200 hover:opacity-80 focus:opacity-80"
        @click="dismiss">
        <span class="sr-only">{{ closeTooltipLabel }}</span>
        <Icon :icon="IconClose" size="xxs" aria-hidden="true" />
      </button>
    </span>
  </div>
</template>

<script>
import Icon from "./icon";

import IconClose from "../icons/close";

let instanceCounter = 0;

export default {
  components: {
    Icon
  },
  props: {
    position: {
      type: String,
      default: "top",
      validation(value) {
        return ["top", "bottom", "right", "left"].includes(value);
      }
    },
    label: {
      type: String,
      default: ""
    },
    closeTooltipLabel: {
      type: String,
      default: "Faire disparaître l'info-bulle"
    },
    hideTooltip: Boolean
  },
  data() {
    return {
      IconClose,
      uniqueId: `pf-tooltip-${++instanceCounter}`
    };
  },
  mounted() {
    document.addEventListener("keydown", this.onEscape);
  },
  unmounted() {
    document.removeEventListener("keydown", this.onEscape);
  },
  methods: {
    dismiss() {
      this.$refs.tooltip.classList.add("hidden");
    },
    onEscape(e) {
      let tooltip = this.$refs.tooltip;
      if (e.key === "Escape" && window.getComputedStyle(tooltip).visibility !== "hidden") {
        this.dismiss();
      }
    }
  }
};
</script>

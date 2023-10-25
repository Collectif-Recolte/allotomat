<template>
  <button
    class="pf-button"
    :class="[
      btnStyleClass,
      sizeClass,
      transition ? `pf-transition-${transition.name} pf-transition-${transition.name}--${transition.color}` : '',
      { 'pf-button--grouped': isGrouped },
      { 'pf-button--icon': isIconOnly }
    ]"
    :type="btnType"
    :disabled="isDisabled">
    <ButtonBg :transition="transition" />
    <ButtonContent v-bind="$props">
      <slot></slot>
    </ButtonContent>
  </button>
</template>

<script>
import ButtonBg from "./_background";
import ButtonContent from "./_content";
import { commonBtnProps } from "./commonBtnProps";

export default {
  components: {
    ButtonBg,
    ButtonContent
  },
  props: {
    ...commonBtnProps,
    btnType: {
      type: String,
      default: "button"
    }
  },
  computed: {
    sizeClass() {
      if (this.size === "sm") {
        return "pf-button--sm";
      } else if (this.size === "lg") {
        return "pf-button--lg";
      }
      return "";
    },
    btnStyleClass() {
      switch (this.btnStyle) {
        case "primary":
          return "pf-button--primary";
        case "secondary":
          return "pf-button--secondary";
        case "outline":
          return "pf-button--outline";
        case "dash":
          return "pf-button--dash";
        case "link":
          return "pf-button--link";
        default:
          return this.btnStyle;
      }
    }
  }
};
</script>

<template>
  <div
    class="pf-note flex justify-between p-4 xs:p-6 md:p-7 shadow-lg"
    :class="[hasBorder ? `border-6 ${borderColorClass}` : '', bgColor, { 'text-white': darkTheme }]">
    <ImgWrapper
      v-if="media"
      v-bind="media"
      height-class="h-16"
      width-class="w-16"
      :is-circle="hasMediaCircle"
      class="flex-shrink-0 mr-4 xs:mr-6 md:mr-7" />

    <div class="h-remove-margin">
      <slot name="title">
        <h5 v-if="title" class="flex items-center text-current">
          <Icon v-if="icon" :icon="icon" class="mr-2" aria-hidden="true" />
          {{ title }}
        </h5>
      </slot>

      <slot name="content" :htmlContent="htmlContent">
        <!-- eslint-disable-next-line vue/no-v-html -->
        <div v-if="htmlContent" class="h-remove-margin" v-html="htmlContent"></div
      ></slot>

      <div v-if="buttonLink || $slots.cta" class="mt-4">
        <slot name="cta">
          <ButtonLink v-if="buttonLink" v-bind="buttonLink" />
        </slot>
      </div>
    </div>
  </div>
</template>

<script>
import ButtonLink from "./button/link";
import Icon from "./icon";
import ImgWrapper from "./_img-wrapper";

export default {
  components: {
    ButtonLink,
    Icon,
    ImgWrapper
  },
  props: {
    bgColorClass: {
      type: String,
      default: ""
    },
    borderColorClass: {
      type: String,
      default: "border-grey-200"
    },
    title: {
      type: String,
      default: ""
    },
    icon: {
      type: Object,
      default: null
    },
    htmlContent: {
      type: [String, Array],
      default: ""
    },
    media: {
      type: Object,
      default: null
    },
    buttonLink: {
      type: Object,
      default: null
    },
    hasMediaCircle: Boolean,
    hasBorder: Boolean,
    darkTheme: Boolean
  },
  computed: {
    bgColor() {
      if (this.hasBorder && !this.bgColorClass) {
        return "bg-transparent";
      }
      if (this.bgColorClass) return this.bgColorClass;
      return "bg-grey-200";
    }
  }
};
</script>

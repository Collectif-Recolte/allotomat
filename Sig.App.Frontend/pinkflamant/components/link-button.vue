<template>
  <a
    class="pf-button"
    :class="[
      typeClass,
      sizeClass,
      transition ? `pf-transition-${transition.name} pf-transition-${transition.name}--${transition.color}` : ''
    ]"
    :href="link.src"
    :target="link.target"
    :download="link.isDownloadable">
    <div
      v-if="transition"
      :class="[
        `pf-transition-${transition.name}__bg pf-transition-${transition.name}__bg--${transition.color}`,
        transition.hasAlternateBgColor ? `pf-transition-${transition.name}__bg--bg-secondary` : ''
      ]"></div>
    <div
      class="flex items-center relative"
      :class="transition && transition.hasContentTag ? `pf-transition__content pf-transition-${transition.name}__content` : ''">
      <span
        class="pf-button__content relative inline-flex items-center"
        :class="transition ? `pf-transition__label pf-transition-${transition.name}__label` : ''">
        <Icon
          v-if="icon && hasIconLeft"
          class="relative"
          :class="[{ 'mr-2': size === 'sm' }, { 'mr-2.5': size === 'md' }, { 'mr-3': size === 'lg' }]"
          :icon="icon"
          :size="iconSize" />

        <span :class="{ 'py-1.5 inline-block': typeClass !== 'pf-button--link' }">{{ link.label }}</span>

        <Icon
          v-if="link.isDownloadable"
          :class="[{ 'ml-2': size === 'sm' }, { 'ml-2.5': size === 'md' }, { 'ml-3': size === 'lg' }]"
          :icon="ICON_DOWNLOAD"
          :size="iconSize" />
        <Icon
          v-else-if="link.target"
          class="relative"
          :class="[{ 'ml-2': size === 'sm' }, { 'ml-2.5': size === 'md' }, { 'ml-3': size === 'lg' }]"
          :icon="ICON_EXTERNAL"
          :size="iconSize" />
        <Icon
          v-else-if="icon && !hasIconLeft"
          :class="[{ 'ml-2': size === 'sm' }, { 'ml-2.5': size === 'md' }, { 'ml-3': size === 'lg' }]"
          :icon="icon"
          :size="iconSize"
      /></span>
    </div>
  </a>
</template>

<script>
import Icon from "./icon";
import ICON_EXTERNAL from "../icons/external.json";
import ICON_DOWNLOAD from "../icons/download.json";

export default {
  components: {
    Icon
  },
  props: {
    typeClass: {
      type: String,
      default: "pf-button--primary"
    },
    size: {
      type: String,
      default: "md"
    },
    link: {
      type: Object,
      required: true
    },
    icon: {
      type: Object,
      default: null
    },
    transition: {
      type: Object,
      default: null
    },
    hasIconLeft: Boolean
  },
  data() {
    return {
      ICON_EXTERNAL,
      ICON_DOWNLOAD
    };
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
    iconSize() {
      if (this.size === "sm") {
        return "xs";
      } else if (this.size === "lg") {
        return "md";
      }
      return "sm";
    }
  }
};
</script>

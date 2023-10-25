<template>
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
        :size="computedIconSize"
        aria-hidden="true" />

      <span :class="{ 'inline-block': btnStyle !== 'link' }">
        <slot>{{ label }}</slot>
        <span v-if="btnScreenReaderAddon" class="sr-only">{{ btnScreenReaderAddon }}</span>
      </span>

      <Icon
        v-if="isDownloadable"
        :class="[{ 'ml-2': size === 'sm' }, { 'ml-2.5': size === 'md' }, { 'ml-3': size === 'lg' }]"
        :icon="ICON_DOWNLOAD"
        :size="computedIconSize"
        aria-hidden="true" />
      <Icon
        v-else-if="target"
        class="relative"
        :class="[{ 'ml-2': size === 'sm' }, { 'ml-2.5': size === 'md' }, { 'ml-3': size === 'lg' }]"
        :icon="ICON_EXTERNAL"
        :size="computedIconSize"
        aria-hidden="true" />
      <Icon
        v-else-if="icon && !hasIconLeft"
        :class="[
          { 'ml-2': size === 'sm' && !isIconOnly },
          { 'ml-2.5': size === 'md' && !isIconOnly },
          { 'ml-3': size === 'lg' && !isIconOnly }
        ]"
        :icon="icon"
        :size="computedIconSize"
        aria-hidden="true"
    /></span>
  </div>
</template>

<script>
import Icon from "../icon";
import { commonBtnProps } from "./commonBtnProps";
import ICON_EXTERNAL from "../../icons/external.json";
import ICON_DOWNLOAD from "../../icons/download.json";

export default {
  components: {
    Icon
  },
  props: {
    ...commonBtnProps,
    src: {
      type: String,
      default: ""
    },
    target: {
      type: String,
      default: null
    },
    isDownloadable: Boolean
  },
  data() {
    return {
      ICON_EXTERNAL,
      ICON_DOWNLOAD
    };
  },
  computed: {
    computedIconSize() {
      if (this.iconSize) {
        return this.iconSize;
      } else if (this.size === "sm") {
        return "sm";
      } else if (this.size === "lg") {
        return "lg";
      } else if (this.isIconOnly) {
        return "sm";
      }
      return "md";
    },
    btnScreenReaderAddon() {
      if (this.screenReaderAddon) return this.screenReaderAddon;
      if (this.target) return "(le lien ouvre dans un nouvel onglet/fenêtre)";
      if (this.isDownloadable) return "(téléchargement du fichier)";
      return null;
    }
  }
};
</script>

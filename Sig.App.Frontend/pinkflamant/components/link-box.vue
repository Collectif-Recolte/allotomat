<template>
  <component :is="tag" :href="link.src" :target="link.target" :download="link.isDownloadable" class="no-underline">
    <div
      class="pf-link-box flex items-center justify-between cursor-pointer relative border-b py-10 transition-colors duration-200 ease-in-out before:absolute before:block before:top-1/2 before:left-1/2 before:transition-colors before:duration-200 before:ease-in-out before:transform before:-translate-x-1/2 before:-translate-y-1/2"
      :class="[
        colors.border,
        colors.text,
        colors.hoverText,
        colors.focusText,
        colors.beforeBg,
        colors.hoverBeforeBg,
        colors.focusBeforeBg,
        hasBleed ? `pf-link-box--bleed` : 'px-4 sm:px-6 before:w-full'
      ]">
      <div class="mr-6 relative flex items-center">
        <Icon v-if="link.iconLeft" ref="icon" :icon="link.iconLeft" :size="link.iconSize" class="mr-2" aria-hidden="true" />
        <h4 class="text-current my-0 mr-6 relative">
          {{ link.title }}
          <span v-if="linkScreenReaderAddon" class="sr-only">{{ linkScreenReaderAddon }}</span>
        </h4>
      </div>

      <div class="flex-shrink-0 relative">
        <Icon v-if="link.isDownloadable" ref="icon" :icon="ICON_DOWNLOAD" :size="link.iconSize" aria-hidden="true" />
        <Icon v-else-if="link.target" class="relative" :icon="ICON_EXTERNAL" :size="link.iconSize" aria-hidden="true" />
        <Icon v-else-if="link.iconRight" ref="icon" :icon="link.iconRight" :size="link.iconSize" aria-hidden="true" />
      </div>
    </div>
  </component>
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
    colorClass: {
      type: Object,
      default() {
        return {};
      }
    },
    tag: {
      type: String,
      default: "a"
    },
    link: {
      type: Object,
      required: true
    },
    screenReaderAddon: {
      type: String,
      default: null
    },
    darkTheme: Boolean,
    hasBleed: Boolean
  },
  data() {
    return {
      ICON_EXTERNAL,
      ICON_DOWNLOAD
    };
  },
  computed: {
    colors() {
      const colors = {
        border: "border-primary-700",
        text: "text-primary-700",
        hoverText: "hover:text-white",
        focusText: "focus:text-white",
        beforeBg: "",
        hoverBeforeBg: "hover:before:bg-primary-500",
        focusBeforeBg: "focus:before:bg-primary-500"
      };

      if (!this.colorClass) return colors;

      let newColors = {};
      for (var key in colors) {
        if (this.colorClass[key]) {
          newColors[key] = this.colorClass[key];
        } else {
          newColors[key] = colors[key];
        }
      }
      return newColors;
    },
    linkScreenReaderAddon() {
      if (this.screenReaderAddon) return this.screenReaderAddon;
      if (this.link.target) return "(le lien ouvre dans un nouvel onglet/fenêtre)";
      if (this.link.isDownloadable) return "(téléchargement du fichier)";
      return null;
    }
  }
};
</script>

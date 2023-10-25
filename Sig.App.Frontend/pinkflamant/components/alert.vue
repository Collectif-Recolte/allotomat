<template>
  <div role="alertdialog" :aria-labelledby="uniqueId">
    <div
      class="py-4 px-6 rounded shadow-lg focus:outline-none ring-primary-500 focus:ring-2"
      :class="[colorTheme.bg, colorTheme.text]"
      role="document"
      tabindex="0">
      <div class="flex items-center justify-between flex-wrap">
        <div class="w-0 flex-1 flex items-center">
          <p :id="uniqueId" class="my-0 text-sm">
            <slot></slot>
          </p>
        </div>
        <div v-if="buttonLink || $slots.right" class="order-3 mt-2 shrink-0 w-full sm:order-2 sm:mt-0 sm:w-auto">
          <slot name="right">
            <ButtonLink
              v-if="buttonLink"
              btn-style="link"
              class="hover:text-current focus:text-current hover:opacity-80 focus:opacity-80"
              :class="colorTheme.accent"
              v-bind="buttonLink" />
          </slot>
        </div>
        <div class="order-2 shrink-0 flex items-center sm:order-3 sm:ml-4">
          <button
            class="relative h-extend-cursor-area p-1.5 -mr-1.5 rounded flex items-center justify-center transition duration-200 ease-in-out hover:opacity-80 focus:opacity-80 focus:outline-none ring-primary-500 focus:ring-2"
            :class="colorTheme.accent"
            @click="$emit('dismiss')">
            <Icon :icon="CLOSE_ICON" size="xs" aria-hidden="true" />
            <span class="sr-only">{{ closeAlertLabel }}</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import CLOSE_ICON from "../icons/close";

import ButtonLink from "./button/link";
import Icon from "./icon";

let instanceCounter = 0;

export default {
  components: { ButtonLink, Icon },
  props: {
    colorTheme: {
      type: Object,
      default() {
        return {
          bg: "bg-primary-50 dark:bg-grey-800",
          text: "text-primary-700 dark:text-primary-300",
          accent: "text-primary-500"
        };
      },
      validator: function (obj) {
        return "bg" in obj && "text" in obj && "accent" in obj;
      }
    },
    buttonLink: {
      type: Object,
      default: null
    },
    closeAlertLabel: {
      type: String,
      default: "Fermer l'alerte"
    }
  },
  emits: ["dismiss"],
  data() {
    return {
      CLOSE_ICON,
      uniqueId: `pf-alert-${++instanceCounter}`
    };
  }
};
</script>

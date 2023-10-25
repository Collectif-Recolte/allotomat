<template>
  <a
    class="pf-sigmund absolute flex items-end right-0 text-p4 text-[10px] leading-tight text-right transition-colors duration-200 ease-in-out"
    :class="textColorClasses"
    href="https://www.sigmund.ca/"
    target="_blank"
    rel="nofollow">
    <!-- eslint-disable-next-line vue/no-v-html -->
    <span class="sr-only xs:not-sr-only" v-html="htmlLabel"></span>
    <Icon :icon="ICON_SIGMUND" size="xl" class="ml-2.5" aria-hidden="true" />
  </a>
</template>

<script>
import ICON_SIGMUND from "../icons/sigmund";
import Icon from "./icon";

export default {
  components: {
    Icon
  },
  props: {
    htmlLabel: {
      type: String,
      default: "Une réalisation<br/>de Sigmund"
    },
    textColorClasses: {
      type: String,
      default: "text-primary-700 hover:text-primary-500 dark:text-primary-300"
    }
  },
  data() {
    return {
      ICON_SIGMUND
    };
  }
};
</script>

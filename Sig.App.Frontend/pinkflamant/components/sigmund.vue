<template>
  <a
    class="pf-sigmund flex items-end text-[8px] xs:text-[10px] leading-tight text-right transition-colors duration-200 ease-in-out"
    :class="textColorClasses"
    href="https://www.sigmund.ca/"
    target="_blank"
    rel="nofollow">
    <!-- eslint-disable-next-line vue/no-v-html -->
    <span v-html="htmlLabel"></span>
    <Icon :icon="ICON_SIGMUND" size="lg" class="xs:hidden ml-2" aria-hidden="true" />
    <Icon :icon="ICON_SIGMUND" size="xl" class="hidden xs:inline ml-2.5" aria-hidden="true" />
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

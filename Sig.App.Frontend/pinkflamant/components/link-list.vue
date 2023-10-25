<template>
  <div class="pf-link-list">
    <slot name="title">
      <h3 v-if="title">{{ title }}</h3>
    </slot>

    <div v-for="(link, index) in links" :key="index">
      <LinkBox :link="link" :color-class="colorClass" :has-bleed="hasBleed" />
    </div>
  </div>
</template>

<script>
import LinkBox from "./link-box";

export default {
  components: {
    LinkBox
  },
  props: {
    title: {
      type: String,
      default: ""
    },
    links: {
      type: Array,
      default: null,
      required: true
    },
    colorClass: {
      type: Object,
      default() {
        return null;
      }
    },
    hasBleed: Boolean
  }
};
</script>

<template>
  <div
    class="pf-intro"
    :class="[
      { 'h-dark-theme': darkTheme },
      { 'xs:flex xs:justify-between xs:mx-0 xs:text-left': isFlex },
      { 'text-center mx-auto': isCentered }
    ]">
    <div class="max-w-2xl" :class="{ 'mx-auto': isCentered && !isFlex }">
      <slot name="suptitle">
        <span v-if="suptitle" class="text-grey-500 uppercase text-p3 tracking-wide">
          {{ suptitle }}
        </span>
      </slot>

      <slot name="title">
        <h2 v-if="title" class="my-0">{{ title }}</h2>
      </slot>

      <div v-if="htmlDescription || $slots.description" class="h-remove-margin mt-6">
        <slot name="description" :htmlDescription="htmlDescription">
          <!-- eslint-disable-next-line vue/no-v-html -->
          <div class="h-remove-margin" v-html="htmlDescription"></div>
        </slot>
      </div>
    </div>
    <div v-if="buttonLink || $slots.cta" class="flex-shrink-0 mt-6" :class="{ 'xs:mt-0 xs:ml-12': isFlex }">
      <slot name="cta">
        <ButtonLink v-if="buttonLink" v-bind="buttonLink" />
      </slot>
    </div>
  </div>
</template>

<script>
import ButtonLink from "./button/link";

export default {
  components: {
    ButtonLink
  },
  props: {
    title: {
      type: String,
      default: ""
    },
    suptitle: {
      type: String,
      default: ""
    },
    htmlDescription: {
      type: [String, Array],
      default: ""
    },
    buttonLink: {
      type: Object,
      default: null
    },
    darkTheme: Boolean,
    isCentered: Boolean,
    isFlex: Boolean
  }
};
</script>

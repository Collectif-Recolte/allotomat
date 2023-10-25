<template>
  <div
    class="pf-tile flex h-full"
    :class="[
      { 'items-center text-center': isCentered, 'group cursor-pointer': isClickable, 'flex-col': !isTwoCols },
      bgColorClass ? `${bgColorClass} px-6 py-10 rounded-xl shadow-lg xs:px-8 md:px-12` : ''
    ]">
    <!-- Image -->
    <div
      v-if="media"
      class="relative flex-shrink-0 flex items-center justify-center overflow-hidden h-16 w-16"
      :class="[
        !isTwoCols && hasContent ? 'mb-4' : 'mr-4 xs:mr-6 md:mr-7',
        mediaBgColorClass,
        hasMediaCircle ? 'rounded-full' : 'rounded',
        { 'transition-opacity duration-200 ease-in-out group-hover:opacity-80 group-focus:opacity-80': isClickable }
      ]">
      <Picture :src="media.src" :alt="media.alt" :breakpoints="media.breakpoints" is-contained />
    </div>
    <div class="h-remove-margin">
      <!-- Titre -->
      <slot name="title">
        <h4 v-if="title" class="mb-2" :class="{ 'text-white': isDarkTheme }">
          {{ title }}
        </h4>
      </slot>

      <!-- Description -->
      <slot name="content">
        <!-- eslint-disable-next-line vue/no-v-html -->
        <div v-if="htmlContent" class="h-remove-margin" :class="{ 'h-dark-theme': isDarkTheme }" v-html="htmlContent"></div>
      </slot>

      <!-- Bouton -->
      <div v-if="buttonLink || $slots.cta" class="mt-4">
        <slot name="cta">
          <ButtonLink v-if="buttonLink" v-bind="buttonLink" />
        </slot>
      </div>
    </div>
  </div>
</template>

<script>
import Picture from "./_picture";
import ButtonLink from "./button/link";

export default {
  components: {
    ButtonLink,
    Picture
  },
  props: {
    htmlContent: {
      type: [String, Array],
      default: ""
    },
    media: {
      type: Object,
      default: null
    },
    title: {
      type: String,
      default: ""
    },
    buttonLink: {
      type: Object,
      default: null
    },
    bgColorClass: {
      type: String,
      default: ""
    },
    mediaBgColorClass: {
      type: String,
      default: ""
    },
    isCentered: Boolean,
    isClickable: Boolean,
    isTwoCols: Boolean,
    hasMediaCircle: Boolean,
    isDarkTheme: Boolean
  },
  computed: {
    hasContent() {
      return this.title || this.$slots.title || this.htmlContent || this.$slots.content;
    }
  }
};
</script>

<template>
  <div class="relative min-h-20">
    <div
      class="border-b border-primary-200 dark:border-grey-800 lg:divide-x divide-primary-200 w-full relative lg:flex lg:items-stretch">
      <div class="px-section md:px-8 min-w-0 flex flex-col items-start gap-y-1 py-5 border-b border-primary-200 lg:border-0">
        <slot name="title">
          <span v-if="suptitle" class="inline-block uppercase text-p4 font-semibold">{{ props.suptitle }}</span>
          <h1
            class="text-2xl my-0 font-bold leading-7 text-gray-900 dark:text-grey-300 max-w-full sm:text-3xl pb-0.5 sm:truncate">
            {{ props.title }}
          </h1>
        </slot>
      </div>
      <div
        v-if="slots.left || slots.center || slots.right"
        class="flex flex-col xs:flex-row ml-auto mr-0 xs:px-section md:px-8 divide-y xs:divide-y-0 xs:divide-x divide-primary-200">
        <div v-if="slots.left" class="flex items-center px-section xs:pr-6 xs:pl-0 py-3 xs:mt-0">
          <slot name="left"></slot>
        </div>
        <div
          v-if="slots.center"
          class="flex items-center px-section xs:pr-6 py-3 xs:mt-0"
          :class="slots.left ? 'xs:pl-6' : 'xs:pl-0'">
          <slot name="center"></slot>
        </div>
        <div
          v-if="slots.right"
          class="flex items-center px-section xs:pr-0 py-3 xs:mt-0"
          :class="slots.center ? 'xs:pl-6' : 'xs:pl-0'">
          <slot name="right"></slot>
        </div>
      </div>
    </div>
    <div
      v-if="props.subpages || slots.subpagesCta"
      class="flex flex-col sm:flex-row sm:items-center sm:px-section md:px-8 divide-y sm:divide-y-0 divide-primary-200 border-b border-primary-200 dark:border-grey-800">
      <ul class="mx-section sm:mx-0 mb-0 flex gap-x-12 xs:mt-auto">
        <li v-for="(subpage, index) in subpages" :key="index" class="inline-block">
          <RouterLink
            class="subpage-link text-h4 font-semibold text-primary-900 inline-flex h-12 items-center border-b-4 transition-colors duration-300 ease-in-out"
            active-class="subpage-link--is-active"
            :to="subpage.route"
            >{{ subpage.label }}</RouterLink
          >
        </li>
      </ul>
      <div v-if="slots.subpagesCta" class="sm:mr-0 sm:ml-auto py-3 px-section sm:px-0 sm:py-2">
        <slot name="subpagesCta"></slot>
      </div>
    </div>
    <div v-if="slots.bottom" class="px-section md:px-8 border-b border-primary-200 dark:border-grey-800 py-4">
      <slot name="bottom"></slot>
    </div>
  </div>
</template>

<script setup>
import { defineProps, useSlots } from "vue";

const props = defineProps({
  title: {
    type: String,
    default: ""
  },
  suptitle: {
    type: String,
    default: ""
  },
  subpages: {
    type: Array,
    default: null
  }
});

const slots = useSlots();
</script>

<style lang="postcss" scoped>
.subpage-link {
  &:not(.subpage-link--is-active) {
    @apply border-transparent hover:border-primary-300;
  }

  &--is-active {
    @apply border-yellow-500;
  }
}
</style>

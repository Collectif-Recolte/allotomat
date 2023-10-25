<template>
  <RouterLink v-slot="{ href, isActive, navigate }" :to="props.routerLink" custom>
    <a class="menu-item" :class="isActive ? 'menu-item--is-active' : 'menu-item--is-inactive'" :href="href" @click="navigate">
      <PfIcon class="menu-item__icon" :class="{ 'menu-item__icon--is-active': isActive }" :icon="props.icon" aria-hidden="true" />
      {{ props.label }}
    </a>
  </RouterLink>
</template>

<script setup>
import { defineProps } from "vue";

const props = defineProps({
  routerLink: {
    type: Object,
    required: true
  },
  label: {
    type: String,
    required: true
  },
  icon: {
    type: Object,
    default: null
  }
});
</script>

<style lang="postcss" scoped>
.menu-item {
  @apply flex 
  items-center 
  px-2 py-2.5
  leading-tight 
  text-base 
  font-semibold
  rounded-md 
  transition-colors 
  duration-200 
  ease-in-out
  text-primary-700 
  dark:text-primary-300 
  focus:outline-none 
  focus-visible:ring-2 
  focus-visible:ring-primary-500 
  dark:hover:text-primary-300 
  dark:focus-visible:text-primary-300 
  dark:hover:bg-grey-900 
  dark:focus-visible:bg-grey-900;

  &--is-inactive {
    @apply hover:bg-white focus-visible:bg-white;
  }

  &--is-active {
    @apply bg-white dark:bg-grey-900 dark:text-primary-300 cursor-default;
  }

  &__icon {
    @apply mr-3
    shrink-0;

    &--is-active {
      @apply text-yellow-500;
    }
  }
}
</style>

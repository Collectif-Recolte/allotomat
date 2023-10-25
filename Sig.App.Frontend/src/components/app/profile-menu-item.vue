<template>
  <RouterLink v-slot="{ href, isActive, navigate }" :to="props.routerPath" custom>
    <MenuItem v-slot="{ active }">
      <a
        class="block text-sm leading-tight px-3 py-2 transition-colors ease-in-out duration-200"
        :class="[
          isActive
            ? 'bg-primary-100 text-black hover:text-black focus:text-black dark:bg-primary-900 dark:text-white dark:hover:text-white dark:focus:text-white'
            : 'text-grey-700 dark:text-grey-200 hover:text-black focus:text-black hover:bg-primary-50 focus:bg-primary-50 dark:hover:text-white dark:focus:text-white dark:hover:bg-primary-700 dark:focus:bg-primary-700',
          { 'bg-primary-50 dark:bg-primary-700 text-black dark:text-white': active }
        ]"
        :href="href"
        @click="navigate">
        {{ props.label }}
      </a>
    </MenuItem>
  </RouterLink>
</template>

<script setup>
import { defineProps } from "vue";
import { MenuItem } from "@headlessui/vue";

const props = defineProps({
  routerPath: { type: Object, default: null },
  label: { type: String, default: null }
});
</script>

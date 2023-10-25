<i18n>
{
	"en": {
		"breadcrumbs": "Breadcrumbs"
	},
	"fr": {
		"breadcrumbs": "Fil d'ariane"
	}
}
</i18n>

<template>
  <nav :aria-label="t('breadcrumbs')">
    <ol role="list" class="mb-0 flex">
      <li v-for="(crumb, index) in props.breadcrumbs" :key="`link-${index}`" class="max-w-40 relative flex pl-3 first:pl-0">
        <RouterLink
          v-if="crumb.route"
          class="truncate text-sm py-3 pr-8 text-grey-500 dark:text-grey-400 leading-none font-semibold transition-colors duration-200 ease-in-out hover:text-primary-700 focus:text-primary-700 dark:hover:text-primary-300 dark:focus:text-primary-300"
          :to="crumb.route">
          {{ crumb.label }}
        </RouterLink>
        <a
          v-else
          href="#"
          class="truncate text-sm py-3 text-grey-500 dark:text-grey-400 leading-none font-semibold cursor-default hover:text-grey-500 focus:text-grey-500 dark:hover:text-grey-400 dark:focus:text-grey-400"
          aria-current="page">
          {{ crumb.label }}
        </a>
        <div v-if="crumb.route" class="block absolute top-0 right-0 h-full w-5" aria-hidden="true">
          <svg class="h-full w-full text-grey-200 dark:text-grey-700" viewBox="0 0 22 80" fill="none" preserveAspectRatio="none">
            <path d="M0 -2L20 40L0 82" vector-effect="non-scaling-stroke" stroke="currentcolor" stroke-linejoin="round" />
          </svg>
        </div>
      </li>
    </ol>
  </nav>
</template>

<script setup>
import { defineProps } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();
const props = defineProps({ breadcrumbs: { type: [Object], default: null } });
</script>

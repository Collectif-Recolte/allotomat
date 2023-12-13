<i18n>
{
	"en": {
		"inactive-link": "Inactive link"
	},
	"fr": {
		"inactive-link": "Lien inactif"
	}
}
</i18n>

<template>
  <div
    class="border-grey-300 dark:border-grey-700"
    :class="[
      { 'border-b pb-1 mb-1 last:my-0 last:pb-0 last:border-b-0': props.item.divideAfter },
      { 'border-t pt-1 mt-1 first:mt-0 first:pt-0 first:border-t-0': props.item.divideBefore }
    ]">
    <PfTooltip
      v-slot="{ tooltipId }"
      :label="props.item.reason"
      :hide-tooltip="!props.item.disabled || !!props.item.reason === false"
      position="left"
      class="pf-transition-visibility-container relative">
      <button
        v-if="props.item.onClick"
        v-bind="$attrs"
        class="flex items-center text-left rounded-none text-grey-700 dark:text-grey-300 text-sm font-normal leading-tight w-full px-3 py-2 transition-colors ease-in-out duration-200 hover:text-black focus:text-black hover:bg-primary-50 focus:bg-primary-50 disabled:cursor-not-allowed disabled:hover:bg-white"
        :class="{ 'bg-primary-50 text-black dark:bg-primary-700 dark:text-white': props.active }"
        :disabled="props.item.disabled"
        :aria-describedby="tooltipId"
        @click="props.item.onClick">
        <PfIcon v-if="props.item.icon" class="mr-2 text-current shrink-0" size="sm" :icon="props.item.icon" aria-hidden="true" />
        {{ props.item.label }}
      </button>
      <component
        :is="props.item.disabled ? 'span' : 'RouterLink'"
        v-else
        v-bind="$attrs"
        class="flex grow items-center text-sm leading-tight px-3 py-1.5 transition-colors ease-in-out duration-200"
        :class="[
          { 'bg-primary-50 text-black dark:bg-primary-700 dark:text-white': props.active },
          props.item.disabled ? 'cursor-not-allowed text-grey-500' : 'text-primary-900 hover:bg-primary-50 focus:bg-primary-50'
        ]"
        :to="props.item.route"
        :aria-describedby="tooltipId">
        <PfIcon v-if="props.item.icon" class="mr-2 text-current shrink-0" size="sm" :icon="props.item.icon" aria-hidden="true" />
        {{ props.item.label }}
        <span class="sr-only">{{ t("inactive-link") }}</span>
      </component>
    </PfTooltip>
  </div>
</template>

<script>
export default {
  inheritAttrs: false
};
</script>

<script setup>
import { useI18n } from "vue-i18n";
import { defineProps } from "vue";

const { t } = useI18n();

const props = defineProps({
  item: {
    type: Object,
    default() {
      null;
    }
  },
  active: Boolean
});
</script>

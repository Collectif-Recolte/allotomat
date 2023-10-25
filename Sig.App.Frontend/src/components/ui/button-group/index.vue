<i18n>
{
	"en": {
		"menu-open-options": "Open menu"
	},
	"fr": {
		"menu-open-options": "Ouvrir le menu"
	}
}
</i18n>

<template>
  <PfButtonGroup
    :items="firstLevelItems"
    :btn-size="btnSize"
    :tooltip-position="tooltipPosition"
    :btn-custom-classes="btnCustomClasses">
    <template v-if="dropdownItems && dropdownItems.length > 0">
      <Menu v-slot="{ open }" as="div" class="group relative -ml-px first:ml-0 first:rounded-l-md last:rounded-r-md">
        <div>
          <MenuButton class="main-dropdown-btn">
            <span class="sr-only">
              {{ t("menu-open-options") }}
            </span>
            <PfIcon
              class="shrink-0 transition-transform ease duration-300 motion-reduce:transition-none"
              :class="open ? 'rotate-180' : 'rotate-0'"
              size="sm"
              :icon="CHEVRON_ICON"
              aria-hidden="true" />
          </MenuButton>
        </div>
        <Transition v-bind="dropdownMenuTransition">
          <MenuItems
            class="origin-top-right absolute z-20 right-0 mt-2 w-48 rounded-md shadow-lg py-1 bg-white dark:bg-grey-800 focus:outline-none">
            <MenuItem
              v-for="(item, index) in dropdownItems"
              :key="index"
              v-slot="{ active }"
              :disabled="item.disabled && !!item.reason">
              <SubmenuItem :item="item" :active="active" />
            </MenuItem>
          </MenuItems>
        </Transition>
      </Menu>
    </template>
  </PfButtonGroup>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { computed, defineProps } from "vue";

import { Menu, MenuButton, MenuItem, MenuItems } from "@headlessui/vue";
import SubmenuItem from "./submenu-item";
import CHEVRON_ICON from "@/lib/icons/chevron-down.json";

const { t } = useI18n();

const props = defineProps({
  items: {
    type: Array,
    default() {
      [];
    }
  },
  tooltipPosition: {
    type: String,
    default: "top"
  },
  btnSize: {
    type: String,
    default: "sm"
  },
  btnCustomClasses: {
    type: Array,
    default: null
  }
});

const firstLevelItems = computed(() => props.items?.filter((x) => !x.isExtra && (("if" in x && x.if) || !("if" in x))) || []);

const dropdownItems = computed(() => {
  // Filter extra items
  const extraItems = props.items ? props.items.filter((x) => x.isExtra) : [];
  // From those items, filter items on which "if" property is true or inexistant
  const conditionalItems = extraItems ? extraItems.filter((x) => ("if" in x && x.if) || !("if" in x)) : [];
  // From those items, filter items who have a reason to be disabled or are not disabled
  return conditionalItems ? conditionalItems.filter((x) => (x.disabled && x.reason) || !x.disabled || !("disabled" in x)) : [];
});

// Style de transition

const dropdownMenuTransition = {
  enterActiveClass: "transition ease-out duration-100",
  enterFromClass: "opacity-0 scale-95",
  enterToClass: "opacity-100 scale-100",
  leaveActiveClass: "transition ease-in duration-75",
  leaveFromClass: "opacity-100 scale-100",
  leaveToClass: "opacity-0 scale-95"
};
</script>

<style lang="postcss" scoped>
.main-dropdown-btn {
  @apply p-2.5 
  group-first:rounded-l-md group-last:rounded-r-md 
  transition-colors duration-200 ease-in-out 
  border border-primary-500 
  dark:border-primary-300 dark:hover:border-primary-300 
  dark:focus:border-primary-300 
  text-primary-500 dark:text-primary-300 
  dark:hover:text-primary-300 dark:focus:text-primary-300 
  hover:bg-primary-50 focus:bg-primary-50 
  focus:outline-none focus:ring-2 focus:ring-primary-500 
  dark:hover:bg-primary-900 dark:focus:bg-primary-900;
}
</style>

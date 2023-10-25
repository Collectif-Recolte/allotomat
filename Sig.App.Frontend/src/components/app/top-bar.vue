<i18n>
{
	"en": {
		"open-sidebar": "Open sidebar",
		"logo": "Tomat logo"
	},
	"fr": {
		"open-sidebar": "Ouvrir le panneau",
		"logo": "Logo de Tomat"
	}
}
</i18n>

<template>
  <div class="sticky top-0 z-20 bg-primary-700">
    <div class="flex h-16 shrink-0">
      <button
        type="button"
        class="px-4 text-white focus:outline-none focus:ring-2 focus:ring-inset focus:ring-yellow-500 md:hidden"
        @click="emit('click')">
        <span class="sr-only">{{ t("open-sidebar") }}</span>
        <PfIcon size="md" :icon="MENU_ICON" aria-hidden="true" />
      </button>
      <div class="flex-1 pr-4 flex justify-between md:px-4">
        <div>
          <div class="shrink-0 flex items-center pr-4 h-16 md:hidden">
            <img class="h-8 relative -top-0.5" :src="require('@/assets/logo/logo-white.svg')" :alt="t('logo')" />
          </div>
        </div>

        <div v-if="!props.hideProfileMenu" class="ml-4 flex items-center md:ml-6">
          <ProfileMenu class="ml-8" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { defineProps, defineEmits } from "vue";

import ProfileMenu from "@/components/app/profile-menu";

import MENU_ICON from "@/lib/icons/menu.json";

const { t } = useI18n();

const props = defineProps({
  hideProfileMenu: Boolean
});

const emit = defineEmits(["click"]);
</script>

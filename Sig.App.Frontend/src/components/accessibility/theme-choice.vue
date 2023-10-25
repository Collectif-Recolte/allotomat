<i18n lang="json">
{
  "en": {
    "description-disabled": "Currently disabled.",
    "description-enabled": "Currently enabled.",
    "label": "Dark theme"
  },
  "fr": {
    "description-disabled": "Présentement désactivé.",
    "description-enabled": "Présentement activé.",
    "label": "Thème sombre"
  }
}
</i18n>

<template>
  <UiSwitch
    v-model="enabled"
    :label="t('label')"
    :description="enabled ? t('description-enabled') : t('description-disabled')"
    :icon-left="SUN_ICON"
    :icon-right="MOON_ICON"
    :icon-color-class="props.iconColorClass" />
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { ref, defineProps, onMounted, onUnmounted, watchEffect } from "vue";

import SUN_ICON from "@/lib/icons/sun.json";
import MOON_ICON from "@/lib/icons/moon.json";

const { t } = useI18n();

const props = defineProps({
  iconColorClass: {
    type: String,
    default: "text-grey-500 dark:text-white"
  }
});

const enabled = ref((getTheme() || getMediaPreference()) === "dark-theme" ? true : false);

function getTheme() {
  return localStorage.getItem("user-theme");
}

function getMediaPreference() {
  const hasDarkPreference = window.matchMedia("(prefers-color-scheme: dark)").matches;
  return hasDarkPreference ? "dark-theme" : "light-theme";
}

watchEffect(() => {
  const theme = enabled.value ? "dark-theme" : "light-theme";
  setTheme(theme);
});

function setTheme(theme) {
  localStorage.setItem("user-theme", theme);
  if (theme === "dark-theme") {
    document.body.classList.add("dark");
  } else {
    document.body.classList.remove("dark");
  }
}

function setThemeFromSystem(e) {
  e.matches ? (enabled.value = true) : (enabled.value = false);
}

onMounted(() => {
  window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", setThemeFromSystem);
});

onUnmounted(() => {
  window.matchMedia("(prefers-color-scheme: dark)").removeEventListener("change", setThemeFromSystem);
});
</script>

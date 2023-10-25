<template>
  <PfButtonAction
    v-if="locale !== $consts.langs.LANG_FR"
    class="no-underline"
    btn-style="link"
    label="Français"
    @click="setLocale($consts.langs.LANG_FR)" />
  <PfButtonAction
    v-else-if="locale !== $consts.langs.LANG_EN"
    class="no-underline"
    btn-style="link"
    label="English"
    @click="setLocale($consts.langs.LANG_EN)" />
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { setLocale } from "@/lib/i18n";

const { locale } = useI18n();
</script>

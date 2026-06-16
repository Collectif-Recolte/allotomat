<template>
  <PfButtonAction
    v-if="locale !== $consts.langs.LANG_FR"
    btn-style="secondary"
    size="lg"
    label="Français"
    @click="setLocale($consts.langs.LANG_FR)" />
  <PfButtonAction v-else btn-style="secondary" size="lg" label="English" @click="setLocale($consts.langs.LANG_EN)" />
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { setLocale } from "@/lib/i18n";

const { locale } = useI18n();
</script>

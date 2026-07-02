<template>
  <PfButtonAction
    v-if="locale !== $consts.langs.LANG_FR"
    class="!border !border-white !text-white !bg-transparent hover:!bg-white/10"
    btn-style="outline"
    size="lg"
    label="Français"
    @click="setLocale($consts.langs.LANG_FR)" />
  <PfButtonAction
    v-else
    class="!border !border-white !text-white !bg-transparent hover:!bg-white/10"
    btn-style="outline"
    size="lg"
    label="English"
    @click="setLocale($consts.langs.LANG_EN)" />
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { setLocale } from "@/lib/i18n";

const { locale } = useI18n();
</script>

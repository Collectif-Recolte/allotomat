<i18n>
{
  "en": {
    "title": "Kiosk access",
    "password": "Password",
    "continue": "Continue",
    "invalid-password": "Invalid password. Try again."
  },
  "fr": {
    "title": "Accès au kiosque",
    "password": "Mot de passe",
    "continue": "Continuer",
    "invalid-password": "Mot de passe invalide. Réessayez."
  }
}
</i18n>

<template>
  <div class="flex flex-1 flex-col items-center justify-center px-6 py-12 gap-6 max-w-lg mx-auto w-full">
    <h1 class="text-h2 font-semibold text-primary-900 text-center">{{ t("title") }}</h1>
    <form class="w-full space-y-4" @submit.prevent="onSubmit">
      <PfFormInputText
        id="kiosk-password"
        :value="password"
        :label="t('password')"
        input-type="password"
        autocomplete="current-password"
        @input="setPassword" />
      <p v-if="showError" class="text-red-600 text-p3 text-center">{{ t("invalid-password") }}</p>
      <PfButtonAction
        class="w-full"
        size="lg"
        type="submit"
        btn-style="primary"
        :label="t('continue')"
        :is-disabled="!password || loginLoading" />
    </form>
  </div>
</template>

<script setup>
import { defineEmits, defineProps, ref, watch } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const props = defineProps({
  login: { type: Function, required: true },
  loginLoading: Boolean,
  authError: Boolean
});

const emit = defineEmits(["authenticated"]);
const password = ref("");
const showError = ref(false);

watch(
  () => props.authError,
  (value) => {
    showError.value = value;
  }
);

function setPassword(value) {
  password.value = value;
}

async function onSubmit() {
  showError.value = false;
  const success = await props.login(password.value);
  if (success) {
    emit("authenticated");
    return;
  }
  showError.value = true;
}
</script>

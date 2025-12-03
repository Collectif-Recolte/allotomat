<i18n>
{
	"en": {
		"forgot-password": "Forgot your password?",
		"loading": "Loading...",
		"login": "Login",
		"logo": "Tomat logo",
		"market-manager-without-market-warning": "No market is associated with your account. If you believe there has been an error, contact the Tomat team.",
		"organization-manager-without-organization-warning": "No group is associated with your account. If you believe there has been an error, contact the Tomat team.",
		"password": "Password",
		"project-manager-without-project-warning": "No program is associated with your account. If you believe there has been an error, contact the Tomat team.",
		"title": "Tomat",
    "subtitle": "Manage your funds in real time.",
		"username": "Email",
    "check-my-balance": "Check my card balance",
    "support": "Help and support",
    "support-link": "https://info.allotomat.com/user-guide/",
    "about": "About Tomat",
    "about-link": "https://info.allotomat.com/about/",
    "form-title": "Welcome to Tomat",
    "form-subtitle": "Enter your login credentials"
	},
	"fr": {
		"forgot-password": "Mot de passe oublié ?",
		"loading": "En chargement...",
		"login": "Connexion",
		"logo": "Logo de Tomat",
		"market-manager-without-market-warning": "Aucun commerce n'est associé à votre compte. Si vous croyez qu'il y a eu une erreur, communiquez avec l'équipe de Tomat.",
		"organization-manager-without-organization-warning": "Aucun groupe n'est associé à votre compte. Si vous croyez qu'il y a eu une erreur, communiquez avec l'équipe de Tomat.",
		"password": "Mot de passe",
		"project-manager-without-project-warning": "Aucun programme n'est associé à votre compte. Si vous croyez qu'il y a eu une erreur, communiquez avec l'équipe de Tomat.",
		"title": "Tomat",
    "subtitle": "Gérez vos fonds en temps réel.",
		"username": "Courriel",
    "check-my-balance": "Vérification du solde de ma carte",
    "support": "Aide et support",
    "support-link": "https://info.allotomat.com/guide-dutilisation/",
    "about": "À propos de Tomat",
    "about-link": "https://info.allotomat.com/a-propos/",
    "form-title": "Bienvenue sur Tomat",
    "form-subtitle": "Entrez vos identifiants de connexion"
	}
}
</i18n>

<template>
  <div class="bg-primary-100 flex flex-col min-h-[100dvh]">
    <div class="dark absolute sm:relative w-full h-[45dvh] min-h-[240px] after:absolute after:inset-0 after:bg-primary-900/50">
      <img class="absolute inset-0 h-full w-full object-cover" :src="require('@/assets/img/bg-login.jpg')" alt="" />
      <LangSwitch class="absolute top-6 right-section z-10 sm:hidden" />
      <nav class="hidden relative z-10 sm:block px-section py-6">
        <ul class="flex items-center gap-x-4 md:gap-x-8 leading-none">
          <li>
            <PfButtonLink
              class="no-underline"
              btn-style="link"
              has-icon-left
              :icon="ICON_INFO"
              :href="t('about-link')"
              :label="t('about')"
              target="_blank" />
          </li>
          <li>
            <PfButtonLink
              class="no-underline"
              btn-style="link"
              has-icon-left
              :icon="ICON_SUPPORT"
              :href="t('support-link')"
              :label="t('support')"
              target="_blank" />
          </li>
          <li class="ml-auto">
            <PfButtonLink
              class="rounded-full"
              tag="RouterLink"
              btn-style="secondary"
              has-icon-left
              :icon="ICON_HAND_CARD"
              :to="{ name: URL_CARD_CHECK }"
              :label="t('check-my-balance')" />
          </li>
          <li>
            <LangSwitch />
          </li>
        </ul>
      </nav>
    </div>

    <div
      class="relative px-section pt-16 pb-8 w-xl max-w-full mx-auto sm:flex flex-row-reverse justify-end gap-x-16 md:gap-x-20 sm:-mt-56 md:-mt-60">
      <div>
        <h1 class="mt-0 sm:mt-14">
          <span class="sr-only">{{ t("title") }}</span>
          <img
            class="h-16 sm:h-20 md:h-24 mx-auto sm:mx-0 relative sm:-left-[6.5rem] md:-left-32"
            :src="require('@/assets/logo/logo-white.svg')"
            :alt="t('logo')" />
        </h1>
        <p class="hidden sm:block text-h2 font-semibold mt-14">{{ t("subtitle") }}</p>
      </div>
      <div class="bg-white rounded-2xl p-8 sm:mt-5 md:p-12 pb-5 md:pb-5 sm:min-w-96 sm:w-5/12">
        <Form
          v-slot="{ isSubmitting }"
          :validation-schema="validationSchema"
          :initial-values="initialFormValues"
          @submit="onSubmit">
          <PfFormSection>
            <div class="sm:mb-4">
              <h2 class="text-h1 leading-6 font-bold text-primary-900 mt-0 mb-3" aria-describedby="connexionFormDesc">
                {{ t("form-title") }}
              </h2>
              <p id="connexionFormDesc" class="mt-1 mb-0 text-p2 text-primary-700">{{ t("form-subtitle") }}</p>
            </div>
            <Field v-slot="{ field, errors }" name="email">
              <PfFormInputText
                id="email"
                :model-value="field.value"
                :label="t('username')"
                :errors="errors"
                input-type="email"
                @update:modelValue="field.onChange"></PfFormInputText>
            </Field>

            <Field v-slot="{ field, errors }" name="password">
              <PfFormInputText
                id="password"
                :model-value="field.value"
                :label="t('password')"
                :errors="errors"
                input-type="password"
                @update:modelValue="field.onChange"></PfFormInputText>
            </Field>
          </PfFormSection>

          <div class="flex items-center justify-between border-t border-grey-300 pt-5 mt-8">
            <RouterLink
              class="relative h-extend-cursor-area pf-button pf-button--link text-p3"
              :to="{ name: URL_ACCOUNT_FORGOT_PASSWORD }">
              {{ t("forgot-password") }}
            </RouterLink>

            <div>
              <div class="inline-block relative">
                <PfButtonAction btn-style="primary" class="px-8" type="submit" :label="t('login')" :is-disabled="isSubmitting" />
                <div class="absolute -translate-y-1/2 top-1/2 right-1">
                  <PfSpinner v-if="isSubmitting" text-color-class="text-white" :loading-label="t('loading')" is-small />
                </div>
              </div>
            </div>
          </div>
        </Form>
      </div>
    </div>

    <nav class="sm:hidden px-section mb-6">
      <ul class="mb-0 text-center">
        <li class="mb-5">
          <PfButtonLink
            class="rounded-full"
            tag="RouterLink"
            btn-style="secondary"
            has-icon-left
            :icon="ICON_HAND_CARD"
            :to="{ name: URL_CARD_CHECK }"
            :label="t('check-my-balance')" />
        </li>
        <li class="mb-4">
          <PfButtonLink
            class="no-underline"
            btn-style="link"
            has-icon-left
            :icon="ICON_INFO"
            :href="t('about-link')"
            :label="t('about')"
            target="_blank" />
        </li>
        <li>
          <PfButtonLink
            class="no-underline"
            btn-style="link"
            has-icon-left
            :icon="ICON_SUPPORT"
            :href="t('support-link')"
            :label="t('support')"
            target="_blank" />
        </li>
      </ul>
    </nav>

    <div class="bg-white mt-auto">
      <AppFooter />
    </div>
  </div>
</template>

<script setup>
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { object, string } from "yup";
import { onMounted, computed } from "vue";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_ACCOUNT_FORGOT_PASSWORD, URL_CARD_CHECK } from "@/lib/consts/urls";
import AuthenticationService from "@/lib/services/authentication";
import { usePageTitle } from "@/lib/helpers/page-title";

import AppFooter from "@/components/app/footer";
import LangSwitch from "@/components/app/lang-switch";

import ICON_HAND_CARD from "@/lib/icons/hand-card.json";
import ICON_SUPPORT from "@/lib/icons/support.json";
import ICON_INFO from "@/lib/icons/info-2.json";

import {
  ProjectManagerWithoutProjectError,
  MarketManagerWithoutMarketError,
  OrganizationManagerWithoutOrganizationError
} from "@/lib/consts/problems";

const { t } = useI18n();
const { query } = useRoute();
const { addError } = useNotificationsStore();

usePageTitle(t("title"));

const initialFormValues = {
  email: query?.email
};

const validationSchema = computed(() =>
  object({
    email: string().label(t("username")).required().email(),
    password: string().label(t("password")).required()
  })
);

async function onSubmit({ email, password }) {
  await AuthenticationService.login(email, password);
}

onMounted(() => {
  const error = query.error;
  switch (error) {
    case ProjectManagerWithoutProjectError:
      addError(t("project-manager-without-project-warning"));
      break;
    case MarketManagerWithoutMarketError:
      addError(t("market-manager-without-market-warning"));
      break;
    case OrganizationManagerWithoutOrganizationError:
      addError(t("organization-manager-without-organization-warning"));
      break;
  }
});
</script>

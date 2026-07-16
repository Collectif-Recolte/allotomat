<i18n>
{
	"en": {
		"forgot-password": "Forgot your password?",
		"loading": "Loading...",
		"login": "Login",
		"logo": "Tomat logo",
		"market-manager-without-market-warning": "No merchant is associated with your account. If you believe there has been an error, contact the Tomat team.",
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
    "form-subtitle": "Tomat accounts are only for program operators and merchants. If you have a card, you may click the "Check my card balance button" without logging in."
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
    "form-subtitle": "Les comptes Tomat sont réservés aux gestionnaires de programmes et aux marchands. Si vous avez une carte, vous pouvez appuyer sur le bouton «\xa0Vérification du solde de ma carte\xa0» sans vous connecter."
	}
}
</i18n>

<template>
  <div class="flex flex-col bg-primary-100 min-h-[100dvh]">
    <div class="absolute after:absolute sm:relative after:inset-0 after:bg-primary-900/50 w-full h-[45dvh] min-h-[240px] dark">
      <img class="absolute inset-0 w-full h-full object-cover" :src="require('@/assets/img/bg-login.jpg')" alt="" />
      <LangSwitch class="sm:hidden top-6 right-section z-10 absolute" />
      <nav class="hidden sm:block z-10 relative px-section py-6">
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
      class="relative sm:flex flex-row-reverse justify-end gap-x-16 md:gap-x-20 mx-auto sm:-mt-56 md:-mt-60 px-section pt-16 pb-8 w-xl max-w-full">
      <div>
        <h1 class="mt-0 sm:mt-14">
          <span class="sr-only">{{ t("title") }}</span>
          <img
            class="sm:-left-[6.5rem] md:-left-32 relative mx-auto sm:mx-0 h-16 sm:h-20 md:h-24"
            :src="require('@/assets/logo/logo-white.svg')"
            :alt="t('logo')" />
        </h1>
        <p class="hidden sm:block mt-14 font-semibold text-h2">{{ t("subtitle") }}</p>
      </div>
      <div class="bg-white sm:mt-5 p-8 md:p-12 pb-5 md:pb-5 rounded-2xl sm:w-5/12 sm:min-w-96">
        <Form
          v-slot="{ isSubmitting }"
          :validation-schema="validationSchema"
          :initial-values="initialFormValues"
          @submit="onSubmit">
          <PfFormSection>
            <div class="sm:mb-4">
              <h2 class="mt-0 mb-3 font-bold text-h1 text-primary-900 leading-6" aria-describedby="connexionFormDesc">
                {{ t("form-title") }}
              </h2>
              <p id="connexionFormDesc" class="mt-1 mb-0 text-p2 text-primary-700">{{ t("form-subtitle") }}</p>
            </div>
            <Field v-slot="{ field, errors }" name="email">
              <PfFormInputText
                id="email"
                v-bind="field"
                :label="t('username')"
                :errors="errors"
                input-type="email"></PfFormInputText>
            </Field>

            <Field v-slot="{ field, errors }" name="password">
              <PfFormInputText
                id="password"
                v-bind="field"
                :label="t('password')"
                :errors="errors"
                input-type="password"></PfFormInputText>
            </Field>
          </PfFormSection>

          <div class="flex justify-between items-center mt-8 pt-5 border-grey-300 border-t">
            <RouterLink
              class="relative text-p3 h-extend-cursor-area pf-button pf-button--link"
              :to="{ name: URL_ACCOUNT_FORGOT_PASSWORD }">
              {{ t("forgot-password") }}
            </RouterLink>

            <div>
              <div class="inline-block relative">
                <PfButtonAction btn-style="primary" class="px-8" type="submit" :label="t('login')" :is-disabled="isSubmitting" />
                <div class="top-1/2 right-1 absolute -translate-y-1/2">
                  <PfSpinner v-if="isSubmitting" text-color-class="text-white" :loading-label="t('loading')" is-small />
                </div>
              </div>
            </div>
          </div>
        </Form>
      </div>
    </div>

    <nav class="sm:hidden mb-6 px-section">
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

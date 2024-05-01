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
		"title": "Login",
		"username": "Email",
    "check-my-balance": "Check my card balance",
    "support": "Support"
	},
	"fr": {
		"forgot-password": "Mot de passe oublié ?",
		"loading": "En chargement...",
		"login": "Connexion",
		"logo": "Logo de Tomat",
		"market-manager-without-market-warning": "Aucun commerce n'est associé à votre compte. Si vous croyez qu'il y a eu une erreur, communiquez avec l'équipe de Tomat.",
		"organization-manager-without-organization-warning": "Aucun groupe n'est associée à votre compte. Si vous croyez qu'il y a eu une erreur, communiquez avec l'équipe de Tomat.",
		"password": "Mot de passe",
		"project-manager-without-project-warning": "Aucun programme n'est associé à votre compte. Si vous croyez qu'il y a eu une erreur, communiquez avec l'équipe de Tomat.",
		"title": "Connexion",
		"username": "Courriel",
    "check-my-balance": "Vérification du solde de ma carte",
    "support": "Support"
	}
}
</i18n>

<template>
  <div class="min-h-screen relative flex items-center justify-center md:block md:h-screen bg-primary-100 dark:bg-grey-900">
    <div class="min-h-full flex w-full">
      <!-- Left col -->
      <div class="flex-1 flex flex-col justify-center h-screen overflow-hidden md:w-1/2 md:flex-none">
        <div
          class="flex-grow flex flex-col justify-between items-start h-full px-section py-4 xs:py-8 md:px-8 overflow-y-scroll lg:overflow-y-visible">
          <!-- Top row (logo) should have the same height as the bottom row -->
          <div class="h-14 xs:h-8 flex items-start w-full mb-12 justify-between gap-x-4">
            <img class="h-8" :src="require('@/assets/logo/logo-color.svg')" :alt="t('logo')" />
            <PfButtonLink
              class="no-underline text-tertiary-500 hover:text-primary-700"
              btn-style="link"
              has-icon-left
              :icon="ICON_QUESTION"
              href="https://allotomat.notion.site/Support-Tomat-c1f01ce94bd549aa92e0a64b624c5507"
              :label="t('support')"
              target="_blank" />
          </div>

          <!-- Middle row (form) -->
          <div class="mx-auto w-full max-w-sm lg:w-96">
            <h1 class="text-3xl max-w-xs mt-0">{{ t("title") }}</h1>

            <div class="mt-8">
              <div class="mt-6">
                <Form
                  v-slot="{ isSubmitting }"
                  class="space-y-6"
                  :validation-schema="validationSchema"
                  :initial-values="initialFormValues"
                  @submit="onSubmit">
                  <PfFormSection>
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

                  <RouterLink
                    class="relative h-extend-cursor-area pf-button pf-button--link"
                    :to="{ name: URL_ACCOUNT_FORGOT_PASSWORD }"
                    >{{ t("forgot-password") }}</RouterLink
                  >

                  <div>
                    <div class="inline-block relative">
                      <PfButtonAction
                        btn-style="primary"
                        class="px-8"
                        type="submit"
                        size="lg"
                        :label="t('login')"
                        :is-disabled="isSubmitting" />
                      <div class="absolute -translate-y-1/2 top-1/2 right-1">
                        <PfSpinner v-if="isSubmitting" text-color-class="text-white" :loading-label="t('loading')" is-small />
                      </div>
                    </div>
                  </div>
                </Form>
              </div>
            </div>
          </div>

          <!-- Bottom row (Secondary links) -->
          <div
            class="h-14 xs:h-8 flex flex-col items-start gap-y-4 w-full mt-12 xs:flex-row-reverse xs:justify-between xs:items-end xs:gap-y-0 xs:gap-x-4">
            <PfButtonLink
              class="no-underline"
              tag="RouterLink"
              btn-style="link"
              has-icon-left
              :icon="ICON_HAND_CARD"
              :to="{ name: URL_CARD_CHECK }"
              :label="t('check-my-balance')" />
            <LangSwitch />
          </div>
        </div>
      </div>
      <div class="hidden md:block relative w-0 flex-1 after:absolute after:inset-0 dark:after:bg-grey-900/50">
        <!-- Résolutions d'images différentes pour écran md & xl -->
        <img class="absolute inset-0 h-full w-full object-cover lg:hidden" :src="require('@/assets/img/bg-login.jpg')" alt="" />
        <img
          class="absolute inset-0 h-full w-full object-cover hidden lg:inline"
          :src="require('@/assets/img/bg-login.jpg')"
          alt="" />
      </div>
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

import LangSwitch from "@/components/app/lang-switch";

import ICON_HAND_CARD from "@/lib/icons/hand-card.json";
import ICON_QUESTION from "@/lib/icons/question-mark-circle-2.json";

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

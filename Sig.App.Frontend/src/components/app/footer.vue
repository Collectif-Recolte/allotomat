<i18n>
{
	"en": {
		"copyright": "© {year} — Collectif Récolte",
		"realization-of" : "A realization<br/>of Sigmund",
    "privacy-policy": "Privacy policy",
    "terms-of-use": "Terms of use",
    "support": "Support"
	},
	"fr": {
		"copyright": "© {year} — Collectif Récolte",
		"realization-of" : "Une réalisation<br/>de Sigmund",
    "privacy-policy": "Politique de confidentialité",
    "terms-of-use": "Conditions d'utilisation",
    "support": "Support"
	}
}
</i18n>

<template>
  <footer class="bg-transparent relative">
    <div class="px-section md:px-8 border-t border-grey-200 dark:border-primary-500 h-24 lg:h-16">
      <div class="h-full flex flex-col justify-center sm:flex-row sm:gap-x-7 sm:justify-start">
        <div class="flex gap-x-7 items-center mb-3 sm:mb-7 lg:mb-0">
          <p class="mb-0 text-xs text-grey-600 dark:text-primary-300">
            {{ t("copyright", { year: new Date().getFullYear() }) }}
          </p>
          <a
            class="text-p3 font-normal pf-button pf-button--link"
            href="https://allotomat.notion.site/Support-Tomat-c1f01ce94bd549aa92e0a64b624c5507"
            target="_blank">
            <span class="inline-flex items-center gap-x-2">
              <PfIcon class="relative top-px" :icon="ICON_QUESTION" size="sm" />
              {{ t("support") }}
            </span>
          </a>
        </div>
        <div class="flex gap-x-7 items-center mr-6 sm:mb-7 lg:mb-0">
          <!-- TODO liens vers les pdf -->
          <a
            class="text-p3 font-normal text-left pf-button pf-button--link"
            :href="`/files/PrivacyPolicy-${locale}-20240416.pdf`"
            target="_blank"
            >{{ t("privacy-policy") }}</a
          >
          <a
            class="text-p3 font-normal text-left pf-button pf-button--link"
            :href="`/files/TermsOfUse-${locale}-20240408.pdf`"
            target="_blank"
            >{{ t("terms-of-use") }}</a
          >
        </div>
      </div>

      <PfSigmund class="bottom-2 xs:bottom-4" :html-label="t('realization-of')" />
    </div>
  </footer>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import ICON_QUESTION from "@/lib/icons/question-mark-circle-2.json";
const { t, locale } = useI18n();
</script>

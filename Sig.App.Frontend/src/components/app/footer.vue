<i18n>
{
	"en": {
		"copyright": "© {year} — Collectif Récolte",
		"realization-of" : "A realization<br/>of Sigmund",
    "privacy-policy": "Privacy policy",
    "terms-of-use": "Terms of use",
    "support": "Support",
    "collectif-recolte-link": "https://recolte.ca/home/"
	},
	"fr": {
		"copyright": "© {year} — Collectif Récolte",
		"realization-of" : "Une réalisation<br/>de Sigmund",
    "privacy-policy": "Politique de confidentialité",
    "terms-of-use": "Conditions d'utilisation",
    "support": "Support",
    "collectif-recolte-link": "https://recolte.ca/accueil/"
	}
}
</i18n>

<template>
  <footer class="bg-transparent relative">
    <div
      class="px-4 xs:mx-0 xs:pl-4 xs:pr-0 md:pl-8 border-t border-grey-200 dark:border-primary-500 h-footer flex items-center justify-center xs:justify-stretch">
      <div
        class="flex flex-wrap gap-x-7 gap-y-3 justify-center xs:justify-start items-center max-w-96 xs:max-w-none mx-auto xs:mx-0 xs:w-full">
        <p class="order-4 xs:order-1 mb-0 text-xs text-grey-600 dark:text-primary-300">
          <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
          {{ new Date().getFullYear() }} —
          <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
          <a class="text-p3 font-normal text-left pf-button pf-button--link" :href="t('collectif-recolte-link')" target="_blank">
            Collectif Récolte
          </a>
        </p>
        <a
          class="order-2 xs:ml-auto text-p3 font-normal text-left pf-button pf-button--link"
          :href="`/files/PrivacyPolicy-${locale}-20240416.pdf`"
          target="_blank"
          >{{ t("privacy-policy") }}</a
        >
        <a
          class="order-3 text-p3 font-normal text-left pf-button pf-button--link"
          :href="`/files/TermsOfUse-${locale}-20240408.pdf`"
          target="_blank"
          >{{ t("terms-of-use") }}</a
        >
        <PfSigmund class="order-5" :html-label="t('realization-of')" />
      </div>
    </div>
  </footer>
</template>

<script setup>
import { useI18n } from "vue-i18n";
const { t, locale } = useI18n();
</script>

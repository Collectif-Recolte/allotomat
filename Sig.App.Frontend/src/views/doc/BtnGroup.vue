<i18n>
{
	"en": {
		"a-disabled-action": "A disabled action",
		"a-link": "A link",
		"an-action": "An action",
		"an-inactive-link": "An inactive link",
		"another-action": "Another action",
		"edit-profile": "Edit profile",
		"send-email": "Send email",
		"title": "Example of buttons group"
	},
	"fr": {
		"a-disabled-action": "Une action désactivée",
		"a-link": "Un lien",
		"an-action": "Une action",
		"an-inactive-link": "Un lien inactif",
		"another-action": "Une autre action",
		"edit-profile": "Éditer le profil",
		"send-email": "Envoyer un courriel",
		"title": "Exemple de groupe de boutons"
	}
}
</i18n>

<template>
  <AppShell :title="t('title')">
    <div>
      <div class="w-xs max-w-full mb-16">
        <UiButtonGroup :items="items" />
      </div>
      <div class="w-xs max-w-full">
        <UiButtonGroup :items="itemsWithoutExtra" />
      </div>
    </div>
  </AppShell>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { ref } from "vue";

import UiButtonGroup from "@/components/ui/button-group/index.vue";

import PENCIL_ICON from "@/lib/icons/pencil.json";
import MAIL_ICON from "@/lib/icons/mail.json";

import { URL_ADMIN_USERS } from "@/lib/consts/urls";
import { usePageTitle } from "@/lib/helpers/page-title";
import { URL_ROOT } from "@/lib/consts/urls";

const { t } = useI18n();

usePageTitle(t("title"));

const items = ref([
  {
    icon: MAIL_ICON,
    label: t("send-email"),
    onClick: () => console.log("Envoyer un email") // eslint-disable-line
  },
  {
    icon: PENCIL_ICON,
    label: t("edit-profile"),
    route: { name: URL_ROOT }
  },
  {
    isExtra: true,
    icon: PENCIL_ICON,
    label: t("an-action"),
    divideAfter: true,
    onClick: () => console.log("Un bouton d'action") // eslint-disable-line
  },
  {
    isExtra: true,
    icon: PENCIL_ICON,
    label: t("another-action"),
    onClick: () => console.log("Un autre bouton d'action") // eslint-disable-line
  },
  {
    isExtra: true,
    icon: PENCIL_ICON,
    label: t("a-disabled-action"),
    onClick: () => console.log("Un autre bouton d'action"), // eslint-disable-line
    disabled: true
  },
  {
    isExtra: true,
    icon: PENCIL_ICON,
    label: t("a-link"),
    route: { name: URL_ADMIN_USERS },
    divideBefore: true,
    reason: "C'est un test"
  },
  {
    isExtra: true,
    icon: PENCIL_ICON,
    label: t("an-inactive-link"),
    route: { name: "" },
    disabled: true,
    reason: "Pas disponible pour le moment"
  },
  {
    isExtra: true,
    icon: PENCIL_ICON,
    label: "Je suis caché.",
    route: { name: "" },
    if: false
  },
  {
    icon: PENCIL_ICON,
    label: "Je suis caché.",
    route: { name: "" },
    if: false
  }
]);

const itemsWithoutExtra = ref([
  {
    icon: MAIL_ICON,
    label: t("send-email"),
    onClick: () => console.log("Envoyer un email") // eslint-disable-line
  },
  {
    icon: PENCIL_ICON,
    label: t("edit-profile"),
    route: { name: URL_ROOT }
  }
]);
</script>

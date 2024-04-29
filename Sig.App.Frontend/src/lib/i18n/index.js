import { createI18n } from "vue-i18n";
import merge from "lodash/merge";

import { LANG_FR, LANG_EN } from "@/lib/consts/langs";
import { LOCAL_STORAGE_LOCALE } from "@/lib/consts/local-storage";

const supportedLocales = [LANG_FR, LANG_EN];
let initialLocale = localStorage.getItem(LOCAL_STORAGE_LOCALE);

if (!initialLocale) {
  const browserLang = navigator.language.slice(0, 2);
  if (supportedLocales.includes(browserLang)) {
    initialLocale = browserLang;
  }
}

if (!supportedLocales.includes(initialLocale)) {
  initialLocale = supportedLocales[0];
}

const i18n = createI18n({
  legacy: false,
  locale: initialLocale,
  messages: loadLocaleMessages()
});

setLocale(initialLocale);

export default i18n;

export function setLocale(locale) {
  localStorage.setItem(LOCAL_STORAGE_LOCALE, locale);
  i18n.global.locale.value = locale;

  document.documentElement.setAttribute("lang", locale);
}

function loadLocaleMessages() {
  if (process.env.NODE_ENV === "test") return null;

  const context = require.context("@", true, /\.i18n\.json$/i);
  const i18nMessages = [];
  context.keys().forEach((k) => i18nMessages.push(context(k)));
  return merge(...i18nMessages);
}

export function addMessages(messages) {
  Object.keys(messages).forEach((k) => i18n.global.mergeLocaleMessage(k, messages[k]));
}

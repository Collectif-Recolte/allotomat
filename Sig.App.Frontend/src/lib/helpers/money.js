import { LANG_FR } from "@/lib/consts/langs";
import i18n from "@/lib/i18n";

function getMoneyFormat(amount) {
  if (amount === undefined || amount === null) return "";
  amount = amount.toFixed(2);
  if (i18n.global.locale.value === LANG_FR) {
    return `${amount.toLocaleString("fr-CA").replace(".", ",")}$`;
  } else {
    return `$${amount.toLocaleString("en-US")}`;
  }
}

function getShortMoneyFormat(amount) {
  if (amount === undefined || amount === null) return "";
  amount = amount.toFixed(0);
  if (i18n.global.locale.value === LANG_FR) {
    return `${amount.toLocaleString("fr-CA")}$`;
  } else {
    return `$${amount.toLocaleString("en-US")}`;
  }
}

export { getMoneyFormat, getShortMoneyFormat };

import { LANG_FR } from "@/lib/consts/langs";
import i18n from "@/lib/i18n";

const archivedValueFr = "Archivé";
const archivedValueEn = "Archived";

export function subscriptionName(subscription) {
  if (subscription.isArchived) {
    return subscription.name + (i18n.global.locale.value === LANG_FR ? ` (${archivedValueFr})` : ` (${archivedValueEn})`);
  } else {
    return subscription.name;
  }
}

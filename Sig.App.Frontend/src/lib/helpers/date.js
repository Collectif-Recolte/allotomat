import { format } from "date-fns";
import { frCA, enCA } from "date-fns/locale";
import { LANG_FR, LANG_EN } from "@/lib/consts/langs";
import i18n from "@/lib/i18n";

const regularFormat = 0;
const regularWithTimeFormat = 1;
const textualFormat = 2;
const textualWithTimeFormat = 3;
const serverFormat = 4;

const regularFormatValue = "dd/MM/yyyy";
const regularWithTimeFormatValue = "dd/MM/yyyy HH:mm";
const textualFormatValueFr = "d MMMM yyyy";
const textualFormatValueEn = "MMMM do, yyyy";
const textualWithTimeFormatValueFr = "dd MMMM yyyy, HH:mm:ss";
const textualWithTimeFormatValueEn = "MMMM do, yyyy, HH:mm:ss";
const serverFormatValue = "yyyy-MM-dd";

function getFormat(tokens, lang) {
  switch (tokens) {
    case regularFormat:
      return regularFormatValue;
    case regularWithTimeFormat:
      return regularWithTimeFormatValue;
    case textualFormat: {
      if (lang === frCA) {
        return textualFormatValueFr;
      }
      return textualFormatValueEn;
    }
    case textualWithTimeFormat:
      if (lang === frCA) {
        return textualWithTimeFormatValueFr;
      }
      return textualWithTimeFormatValueEn;
    case serverFormat:
      return serverFormatValue;
    default:
      throw new Error("Invalid date format.");
  }
}

function formatDate(date, formatId) {
  if (date === null) {
    return "";
  }
  var lang = i18n.global.locale.value === LANG_FR ? frCA : i18n.global.locale.value === LANG_EN ? enCA : null;

  const tokens = getFormat(formatId, lang);

  return format(date, tokens, { locale: lang });
}

function dateUtc(date) {
  if (date === null) {
    return null;
  }
  let dateStr = date.substring(0, 10).split("-");
  return new Date(dateStr[0], dateStr[1] - 1, dateStr[2]);
}

export {
  formatDate,
  dateUtc,
  regularFormat,
  regularWithTimeFormat,
  textualFormat,
  textualWithTimeFormat,
  regularFormatValue,
  serverFormat
};

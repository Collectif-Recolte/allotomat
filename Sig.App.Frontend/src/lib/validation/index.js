/* eslint-disable @intlify/vue-i18n/no-missing-keys */
import { setLocale, addMethod, string, number, object, array } from "yup";

import i18n from "@/lib/i18n";

addMethod(string, "password", function () {
  return this.test("is-password", "", function (value) {
    const { label, createError } = this;

    const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[!"#$%&'()*+,-./:;<=>?@[\]^_`{|}~*])(?=.{10,})/;
    return regex.test(value) || createError({ message: i18n.global.t("validator-string-isPassword", { label }) });
  });
});

addMethod(string, "samePassword", function (password) {
  return this.test("is-same-password", "", function (value) {
    const { label, createError } = this;
    return (
      this.resolve(password) === value || createError({ message: i18n.global.t("validator-string-samePassword", { label }) })
    );
  });
});

addMethod(string, "requiredHtml", function () {
  return this.test("required-html", "", function (value) {
    const { label, createError } = this;

    // TODO : Ça ne gère pas les &nbsp; à revoir plus tard
    const regex = /<[^/>][^>]*><\/[^>]+>/gi;
    return (
      value.replace(regex, "").trim() !== "" ||
      createError({ message: i18n.global.t("validator-string-requiredHtml", { label }) })
    );
  });
});

addMethod(number, "between", function (min, max) {
  return this.test("number-between", "", function (value) {
    const { label, createError } = this;

    return (
      (value >= min && value <= max) || createError({ message: i18n.global.t("validator-number-between", { label, min, max }) })
    );
  });
});

addMethod(object, "image", function () {
  return this.test("is-image", "", function (files) {
    const { label, createError } = this;

    if (files === null || files === undefined) {
      return true;
    }

    const error = createError({ message: i18n.global.t("validator-object-isImage", { label }) });
    const regex = /\.(jpg|svg|jpeg|png|bmp|gif|webp)$/i;
    if (Array.isArray(files)) {
      return files.every((file) => regex.test(file.name)) || error;
    }

    return regex.test(files.name) || error;
  });
});

addMethod(array, "unique", function (message, mapper = (a) => a) {
  return this.test("unique", message, function (list) {
    return list.length === new Set(list.map(mapper)).size;
  });
});

// Default error messages configuration
// https://github.com/jquense/yup/blob/master/src/locale.ts
setLocale({
  mixed: {
    default: ({ label }) => i18n.global.t("validator-mixed-default", { label }),
    required: ({ label }) => i18n.global.t("validator-mixed-required", { label }),
    defined: ({ label }) => i18n.global.t("validator-mixed-defined", { label }),
    notNull: ({ label }) => i18n.global.t("validator-mixed-notNull", { label }),
    oneOf: ({ label, values }) => i18n.global.t("validator-mixed-oneOf", { label, values }),
    notOneOf: ({ label, values }) => i18n.global.t("validator-mixed-notOneOf", { label, values }),
    notType: ({ label, type, value, originalValue }) => {
      const castMsg =
        originalValue != null && originalValue !== value
          ? i18n.global.t("validator-mixed-notType-castMsg", { value: originalValue })
          : ".";

      return type !== "mixed"
        ? i18n.global.t("validator-mixed-notType-isNotMixed", { label, type, value, castMsg })
        : i18n.global.t("validator-mixed-notType-isMixed", { label, type, value, castMsg });
    }
  },
  string: {
    length: ({ label, length }) => i18n.global.t("validator-string-length", { label, length }),
    min: ({ label, min }) => i18n.global.t("validator-string-min", { label, min }),
    max: ({ label, max }) => i18n.global.t("validator-string-max", { label, max }),
    matches: ({ label, regex }) => i18n.global.t("validator-string-matches", { label, regex }),
    email: ({ label }) => i18n.global.t(`validator-string-email`, { label }),
    url: ({ label }) => i18n.global.t(`validator-string-url`, { label }),
    uuid: ({ label }) => i18n.global.t(`validator-string-uuid`, { label }),
    trim: ({ label }) => i18n.global.t(`validator-string-trim`, { label }),
    lowercase: ({ label }) => i18n.global.t(`validator-string-lowercase`, { label }),
    uppercase: ({ label }) => i18n.global.t(`validator-string-uppercase`, { label })
  },
  number: {
    min: ({ label, min }) => i18n.global.t("validator-number-min", { label, min }),
    max: ({ label, max }) => i18n.global.t("validator-number-max", { label, max }),
    lessThan: ({ label, less }) => i18n.global.t("validator-number-lessThan", { label, less }),
    moreThan: ({ label, more }) => i18n.global.t("validator-number-moreThan", { label, more }),
    positive: ({ label }) => i18n.global.t("validator-number-positive", { label }),
    negative: ({ label }) => i18n.global.t("validator-number-negative", { label }),
    integer: ({ label }) => i18n.global.t("validator-number-integer", { label })
  },
  date: {
    min: ({ label, min }) => i18n.global.t("validator-date-min", { label, min }),
    max: ({ label, max }) => i18n.global.t("validator-date-max", { label, max })
  },
  boolean: {
    isValue: ({ label, value }) => i18n.global.t("validator-boolean-isValue", { label, value })
  },
  object: {
    noUnknown: ({ label, unknown }) => i18n.global.t("validator-object-noUnknown", { label, unknown })
  },
  array: {
    min: ({ label, min }) => i18n.global.t("validator-array-min", { label, min }),
    max: ({ label, max }) => i18n.global.t("validator-array-max", { label, max }),
    length: ({ label, length }) => i18n.global.t("validator-array-length", { label, length })
  }
});

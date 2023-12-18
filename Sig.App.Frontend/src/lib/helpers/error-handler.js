/* eslint-disable @intlify/vue-i18n/no-missing-keys */
import { getCurrentInstance, onUnmounted } from "vue";
import i18n from "@/lib/i18n";
import Logger from "@/lib/services/logger";
import { useNotificationsStore } from "@/lib/store/notifications";

export function isGraphQLError(err, code) {
  return err.graphQLErrors && !!err.graphQLErrors.find((e) => e.extensions?.code === code || e.extensions?.codes.includes(code));
}

export function VueErrorHandler(err, vm, info) {
  showErrorNotification(err, vm);

  if (!err.__handled) {
    logError(err, vm, info);

    console.error("Unhandled error", { err, vm, info });
    /*if (process.env.NODE_ENV === "development") {
      // eslint-disable-next-line no-console
      console.error("Unhandled error", { err, vm, info });
    }*/
  }
}

const graphQLErrorMessages = new Map();
export function useGraphQLErrorMessages(errorMessages) {
  const instance = getCurrentInstance();
  graphQLErrorMessages.set(instance, errorMessages);
  onUnmounted(() => graphQLErrorMessages.delete(instance));
}

function showErrorNotification(err, vm) {
  const { addError } = useNotificationsStore();

  const errorMessage = getErrorMessage(err, vm);
  addError(errorMessage);
}

function getErrorMessage(err, vm) {
  if (err.graphQLErrors) {
    const errorMessages = err.graphQLErrors.map((x) => getGraphQLErrorMessage(x, vm)).filter((x) => !!x);

    if (errorMessages.length > 0) {
      // If we had a custom error message, we assume it was an expected error
      // and we mark it as handled so that it is not sent to the logger
      err.__handled = true;

      return errorMessages.join("\n");
    }
  }

  return i18n.global.t("error-handler-unhandled");
}

function getGraphQLErrorMessage(graphQLError, vm) {
  if (!vm) return null;

  let customMessage;
  for (const errorCode of [graphQLError.extensions?.code, ...graphQLError.extensions?.codes]) {
    customMessage = graphQLErrorMessages.get(vm.$)?.[errorCode];
    if (customMessage) break;
  }

  if (typeof customMessage === "function") {
    customMessage = customMessage.bind(vm)(graphQLError);
  }

  return customMessage || getGraphQLErrorMessage(graphQLError, vm.$parent);
}

function logError(err, vm, info) {
  let errorMessage = `Unhandled error: ${err}`;

  if (vm && vm.$options) {
    const { name, __file } = vm.$options;

    if (name) {
      errorMessage += `\nComponent name: ${name}`;
    }
    if (__file) {
      errorMessage += `\nFile name: ${__file}`;
    }
  }

  if (info) {
    errorMessage += `\nInfo: ${JSON.stringify(info, null, 2)}`;
  }

  Logger.logError(errorMessage);
}

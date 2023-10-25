import { createApp, h, provide } from "vue";
import { Form, Field, ErrorMessage } from "vee-validate";
import VueGtag from "vue-gtag";
import { DefaultApolloClient } from "@vue/apollo-composable";
import { createPinia } from "pinia";

import App from "@/App.vue";
import AppShell from "@/components/app/app-shell.vue";
import PublicShell from "@/components/app/public-shell.vue";
import { ConstsPlugin } from "@/lib/consts";
import { apolloClient } from "@/lib/graphql/apollo-client";
import { VueErrorHandler } from "@/lib/helpers/error-handler";
import i18n from "@/lib/i18n";
import { LoggerPlugin } from "@/lib/services/logger";
import router from "@/lib/router";

import { VUE_APP_GA_MEASUREMENT_ID } from "@/env";

import "@/assets/styles/tailwind.css";
import "@/lib/validation";

const app = createApp({
  setup() {
    provide(DefaultApolloClient, apolloClient);
  },
  render: () => h(App)
});

app.config.errorHandler = VueErrorHandler;

app.use(i18n);
app.use(router);
app.use(createPinia());

if (VUE_APP_GA_MEASUREMENT_ID !== "") {
  app.use(
    VueGtag,
    {
      config: {
        id: VUE_APP_GA_MEASUREMENT_ID,
        params: {
          anonymize_ip: true
        }
      }
    },
    router
  );
}

app.use(ConstsPlugin);
app.use(LoggerPlugin);

registerGlobalComponents();

app.mount("#app");

function registerGlobalComponents() {
  // Templates
  app.component("AppShell", AppShell);
  app.component("PublicShell", PublicShell);

  // vee-validate
  app.component("Form", Form);
  app.component("Field", Field);
  app.component("ErrorMessage", ErrorMessage);

  // @/components/ui/foo/bar.vue => <UiFooBar />
  const uiCtx = require.context("@/components/ui", true, /\.vue$/);
  registerGlobalComponentsFromContext(uiCtx, "Ui");

  // ../pinkflamant/components/foo/bar.vue => <PfFooBar />
  const pfCtx = require.context("../pinkflamant/components", true, /\.vue$/);
  registerGlobalComponentsFromContext(pfCtx, "Pf");
}

function registerGlobalComponentsFromContext(context: __WebpackModuleApi.RequireContext, prefix: string) {
  context.keys().forEach((k) => {
    // "./some/path.vue" --> "some/path"
    const match = /^.\/(.*)\.vue$/.exec(k);
    if (!match) return;

    const fileNameWithoutExtension = match[1];
    const segments = fileNameWithoutExtension.split("/");

    // Don't register the component if any path segment starts with an underscore
    if (segments.find((x) => x.startsWith("_"))) return;

    // If the last path segment (the filename) is "index", register the component under the parent folder's name
    // ie. some/path/index is imported as SomePath
    const lastSegment = segments[segments.length - 1];
    if (lastSegment.toLowerCase() === "index") segments.pop();

    const component = context(k).default;
    const componentName = prefix + segments.map(toPascalCase).join("");

    app.component(componentName, component);
  });
}

// https://stackoverflow.com/a/54651317/5903
function toPascalCase(text: string) {
  return text.replace(/(^\w|-\w|\/\w)/g, clearAndUpper);
}
function clearAndUpper(text: string) {
  return text.replace(/[-/]/, "").toUpperCase();
}

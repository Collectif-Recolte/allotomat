const overrides = window.app_env || {};

export const VUE_APP_ROOT_API = overrides.VUE_APP_ROOT_API || process.env.VUE_APP_ROOT_API || "";
export const VUE_APP_GRAPHQL_HTTP = overrides.VUE_APP_GRAPHQL_HTTP || process.env.VUE_APP_GRAPHQL_HTTP || "/graphql";
export const VUE_APP_GA_MEASUREMENT_ID = overrides.VUE_APP_GA_MEASUREMENT_ID || process.env.VUE_APP_GA_MEASUREMENT_ID || "";

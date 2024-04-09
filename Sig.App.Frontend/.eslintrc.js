module.exports = {
  root: true,

  env: {
    node: true
  },

  plugins: ["eslint-plugin-local-rules"],

  extends: [
    "plugin:vue/vue3-recommended",
    "eslint:recommended",
    "@vue/typescript",
    "@vue/prettier",
    "@vue/prettier/@typescript-eslint",
    "plugin:@intlify/vue-i18n/recommended"
  ],

  parserOptions: {
    parser: "@typescript-eslint/parser"
  },

  rules: {
    "no-console": "warn",
    "no-debugger": "warn",
    "local-rules/format-graphql-block": 1,
    "@intlify/vue-i18n/no-duplicate-keys-in-locale": "warn",
    "@intlify/vue-i18n/no-missing-keys-in-other-locales": "warn",
    "@intlify/vue-i18n/key-format-style": ["warn", "kebab-case"],
    "@intlify/vue-i18n/no-unused-keys": "off",
    "@intlify/vue-i18n/no-html-messages": "off"
  },

  settings: {
    "vue-i18n": {
      localeDir: {
        pattern: "./src/**/*.i18n.*.json",
        localePattern: /.i18n.(?<locale>[a-z]{2}).json/i,
        localeKey: "path"
      }
    }
  },

  overrides: [
    {
      files: ["*.graphql"],
      plugins: ["@graphql-eslint"],
      parserOptions: {
        parser: "@graphql-eslint/eslint-plugin"
      }
    },
    {
      files: ["**/__tests__/*.{j,t}s?(x)", "**/tests/unit/**/*.spec.{j,t}s?(x)"],
      env: {
        jest: true
      }
    }
  ]
};

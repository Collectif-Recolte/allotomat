module.exports = () => ({
  preset: "@vue/cli-plugin-unit-jest/presets/typescript-and-babel",
  globals: {
    "vue-jest": {
      transform: {
        i18n: "./tests/vue-i18n-jest"
      }
    }
  },
  moduleNameMapper: {
    "^@tests/(.*)$": "<rootDir>/tests/$1"
  }
});

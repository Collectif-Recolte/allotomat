const merge = require("lodash/merge");

module.exports = {
  content: ["./src/**/*.html", "./src/**/*.js", "./src/**/*.vue", "./pinkflamant/**/*.vue"],
  darkMode: "class",
  theme: merge(require("./pinkflamant/theme"), require("./src/theme")),
  plugins: [
    require("@tailwindcss/forms"),
    require("tailwindcss-scoped-groups")({
      groups: ["one", "two", "pfone", "pftwo"]
    })
  ]
};

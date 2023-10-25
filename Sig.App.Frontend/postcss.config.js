module.exports = {
  plugins: {
    "postcss-import": {},
    "tailwindcss/nesting": {},
    "postcss-hexrgba": {},
    tailwindcss: { config: "./tailwind.config.js" },
    autoprefixer: {},
    ...(process.env.NODE_ENV === "production" ? { cssnano: {} } : {})
  }
};

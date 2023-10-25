module.exports = {
  devServer: {
    host: "localhost",
    port: 8064,
    https: false
  },
  chainWebpack: (config) => {
    // Handle .graphql files
    config.module
      .rule("graphql")
      .test(/\.graphql$/)
      .use("graphql-tag/loader")
      .loader("graphql-tag/loader")
      .end();

    // Handle <i18n> custom blocks
    config.module
      .rule("i18n-block")
      .resourceQuery(/blockType=i18n/)
      .type("javascript/auto")
      .use()
      .loader("@intlify/vue-i18n-loader")
      .end();
  }
};

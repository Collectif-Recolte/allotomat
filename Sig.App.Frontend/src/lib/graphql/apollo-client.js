import { ApolloClient, createHttpLink, InMemoryCache } from "@apollo/client/core";

import { VUE_APP_GRAPHQL_HTTP } from "@/env";

import possibleTypes from "./possibleTypes.json";

// HTTP connection to the API
const httpLink = createHttpLink({
  // You should use an absolute URL here
  uri: VUE_APP_GRAPHQL_HTTP
});

const cache = new InMemoryCache({
  possibleTypes
});

const defaultOptions = {
  query: {
    fetchPolicy: "network-only"
  },
  watchQuery: {
    fetchPolicy: "cache-and-network"
  }
};

// Create the apollo client
export const apolloClient = new ApolloClient({
  link: httpLink,
  cache,
  defaultOptions
});

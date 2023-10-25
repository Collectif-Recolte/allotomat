import { ApolloClient, InMemoryCache } from "@apollo/client/core";
import { SchemaLink } from "@apollo/client/link/schema";
import { makeExecutableSchema } from "@graphql-tools/schema";
import { addMocksToSchema } from "@graphql-tools/mock";

export function createTestingApollo({ typeDefs, resolvers, mocks }) {
  const schema = makeExecutableSchema({ typeDefs, resolvers });
  const schemaWithMocks = addMocksToSchema({ schema, mocks, preserveResolvers: true });

  const cache = new InMemoryCache();
  const link = new SchemaLink({ schema: schemaWithMocks });

  return new ApolloClient({ cache, link });
}

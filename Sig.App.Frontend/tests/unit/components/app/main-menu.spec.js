import { flushPromises, shallowMount } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";
import { DefaultApolloClient } from "@vue/apollo-composable";

import { createTestingApollo } from "@tests/test-apollo-client";

import MainMenu from "@/components/app/main-menu";
import { ConstsPlugin } from "@/lib/consts";
import i18n from "@/lib/i18n";
import { useAuthStore } from "@/lib/store/auth";

jest.mock("@/lib/services/authentication");

const typeDefs = `
  type Profile {
    id: ID!
    firstName: String
    lastName: String
  }
  type User {
    id: ID!
    email: String!
    profile: Profile
  }
  type Query {
    me: User
  }
`;

describe("main-menu.vue", () => {
  let mountOptions;
  let mockedUser;

  beforeEach(() => {
    const resolvers = {
      Query: { me: () => mockedUser }
    };

    const apolloClient = createTestingApollo({ typeDefs, resolvers });

    mountOptions = {
      global: {
        plugins: [i18n, createTestingPinia(), ConstsPlugin],
        provide: {
          [DefaultApolloClient]: apolloClient
        },
        stubs: {
          RouterLink: true
        }
      }
    };
  });

  it.skip("shows the user's name", async () => {
    const authStore = useAuthStore();
    authStore.claims = {};

    mockedUser = {
      id: "1",
      email: "john.doe@example.com",
      profile: {
        id: "1",
        firstName: "John",
        lastName: "Doe"
      }
    };

    const wrapper = shallowMount(MainMenu, mountOptions);
    await flushPromises();

    expect(wrapper.text()).toContain("John Doe");
  });

  it.skip("shows user's email when they have no profile", async () => {
    const authStore = useAuthStore();
    authStore.claims = {};

    mockedUser = {
      id: "1",
      email: "john.doe@example.com",
      profile: null
    };

    const wrapper = shallowMount(MainMenu, mountOptions);
    await flushPromises();

    expect(wrapper.text()).toContain("john.doe@example.com");
  });
});

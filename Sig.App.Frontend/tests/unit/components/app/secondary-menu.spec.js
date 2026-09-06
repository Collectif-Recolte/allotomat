import { mount } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";
import { DefaultApolloClient } from "@vue/apollo-composable";
import { createTestingApollo } from "@tests/test-apollo-client";
import SecondaryMenu from "@/components/app/secondary-menu";
import { ConstsPlugin } from "@/lib/consts";
import i18n from "@/lib/i18n";
import { useAuthStore } from "@/lib/store/auth";
import { CLAIM_UTYPE, CLAIM_UTYPE_ORGANIZATIONMANAGER, CLAIM_UTYPE_PROJECTMANAGER } from "@/lib/consts/claims";
import {
  GLOBAL_MANAGE_BENEFICIARIES,
  GLOBAL_MANAGE_ORGANIZATION_MANAGERS,
  GLOBAL_MANAGE_PROJECT_MANAGERS,
  GLOBAL_MANAGE_SPECIFIC_ORGANIZATION,
  GLOBAL_MANAGE_SPECIFIC_PROJECT,
  GLOBAL_MANAGE_TRANSACTIONS
} from "@/lib/consts/permissions";
import { URL_BUDGET_ALLOWANCE_REPORT, URL_SUBSCRIPTION_END_REPORT } from "@/lib/consts/urls";
const typeDefs = `
  type Project {
    id: ID!
    name: String!
  }
  type MarketGroup {
    id: ID!
    name: String!
  }
  type Market {
    id: ID!
    name: String!
  }
  type Query {
    allProjects: [Project!]!
    marketGroups: [MarketGroup!]!
    markets: [Market!]!
  }
`;
const resolvers = {
  Query: {
    allProjects: () => [{ __typename: "Project", id: "project-1", name: "Programme 1" }],
    marketGroups: () => [],
    markets: () => []
  }
};
const SecondaryMenuItemStub = {
  name: "SecondaryMenuItem",
  props: ["routerLink", "label", "icon"],
  template: '<a :data-route="routerLink && routerLink.name">{{ label }}</a>'
};
function mountMenu(claims, globalPermissions) {
  const apolloClient = createTestingApollo({ typeDefs, resolvers });
  const pinia = createTestingPinia({ createSpy: jest.fn, stubActions: false });
  useAuthStore().initialize(claims, globalPermissions);
  return mount(SecondaryMenu, {
    global: {
      plugins: [i18n, pinia, ConstsPlugin],
      provide: {
        [DefaultApolloClient]: apolloClient
      },
      stubs: {
        SecondaryMenuItem: SecondaryMenuItemStub
      }
    }
  });
}
function reportLinks(wrapper) {
  return wrapper.findAll("a").map((x) => x.attributes("data-route"));
}
const organizationManagerPermissions = [
  GLOBAL_MANAGE_SPECIFIC_ORGANIZATION,
  GLOBAL_MANAGE_BENEFICIARIES,
  GLOBAL_MANAGE_ORGANIZATION_MANAGERS,
  GLOBAL_MANAGE_TRANSACTIONS
];
const projectManagerPermissions = [GLOBAL_MANAGE_SPECIFIC_PROJECT, GLOBAL_MANAGE_PROJECT_MANAGERS, GLOBAL_MANAGE_TRANSACTIONS];
describe("secondary-menu.vue", () => {
  it("offers both reports to a project manager", () => {
    const wrapper = mountMenu({ [CLAIM_UTYPE]: CLAIM_UTYPE_PROJECTMANAGER }, projectManagerPermissions);
    expect(reportLinks(wrapper)).toContain(URL_SUBSCRIPTION_END_REPORT);
    expect(reportLinks(wrapper)).toContain(URL_BUDGET_ALLOWANCE_REPORT);
  });
  // CRCL-2408 : les deux rapports sont offerts aux gestionnaires de programme et de groupe.
  it("offers both reports to an organization manager", () => {
    const wrapper = mountMenu({ [CLAIM_UTYPE]: CLAIM_UTYPE_ORGANIZATIONMANAGER }, organizationManagerPermissions);
    expect(reportLinks(wrapper)).toContain(URL_SUBSCRIPTION_END_REPORT);
    expect(reportLinks(wrapper)).toContain(URL_BUDGET_ALLOWANCE_REPORT);
  });
});

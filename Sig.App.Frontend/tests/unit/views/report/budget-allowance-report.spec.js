import { h } from "vue";
import { flushPromises, mount } from "@vue/test-utils";
import { createTestingPinia } from "@pinia/testing";
import { DefaultApolloClient } from "@vue/apollo-composable";

import { createTestingApollo } from "@tests/test-apollo-client";

import { ConstsPlugin } from "@/lib/consts";
import i18n from "@/lib/i18n";
import { useAuthStore } from "@/lib/store/auth";
import { CLAIM_UTYPE, CLAIM_UTYPE_ORGANIZATIONMANAGER } from "@/lib/consts/claims";
import { GLOBAL_MANAGE_TRANSACTIONS } from "@/lib/consts/permissions";

import BudgetAllowanceReport from "@/views/report/BudgetAllowanceReport.vue";
import BudgetAllowanceReportTable from "@/components/report/budget-allowance-report-table";

jest.mock("vue-router", () => ({
  useRoute: () => ({ query: {} }),
  useRouter: () => ({ replace: jest.fn(), push: jest.fn() })
}));

const typeDefs = `
  scalar DateTime
  type Subscription {
    id: ID!
    name: String!
    isArchived: Boolean
  }
  type BudgetAllowance {
    id: ID!
    subscription: Subscription
  }
  type BudgetAllowanceLog {
    id: ID!
    discriminator: String
    createdAt: DateTime
    amount: Float
    organizationName: String
    subscriptionName: String
    targetOrganizationName: String
    targetSubscriptionName: String
  }
  type BudgetAllowanceReport {
    totalCount: Int!
    totalPages: Int!
    items: [BudgetAllowanceLog!]!
  }
  type Organization {
    id: ID!
    name: String!
    budgetAllowances: [BudgetAllowance!]!
    budgetAllowanceReport(
      page: Int!
      limit: Int!
      startDate: DateTime!
      endDate: DateTime!
      withSpecificSubscriptions: [ID!]
    ): BudgetAllowanceReport
  }
  type Project {
    id: ID!
    reconciliationReportDate: String
    organizations: [Organization!]!
    subscriptions: [Subscription!]!
    budgetAllowanceReport(
      page: Int!
      limit: Int!
      startDate: DateTime!
      endDate: DateTime!
      withSpecificOrganizations: [ID!]
      withSpecificSubscriptions: [ID!]
    ): BudgetAllowanceReport
  }
  type Query {
    projects: [Project!]!
    organizations: [Organization!]!
  }
`;

const subscription = { __typename: "Subscription", id: "sub-1", name: "Feeding Futures 2026", isArchived: false };

const organization = {
  __typename: "Organization",
  id: "org-1",
  name: "Penticton SD67",
  budgetAllowances: [{ __typename: "BudgetAllowance", id: "ba-1", subscription }],
  budgetAllowanceReport: {
    __typename: "BudgetAllowanceReport",
    totalCount: 1,
    totalPages: 1,
    items: [
      {
        __typename: "BudgetAllowanceLog",
        id: "log-1",
        discriminator: "CREATE_BUDGET_ALLOWANCE_LOG",
        createdAt: "2026-08-15T12:00:00Z",
        amount: 35000,
        organizationName: "Penticton SD67",
        subscriptionName: "Feeding Futures 2026",
        targetOrganizationName: null,
        targetSubscriptionName: null
      }
    ]
  }
};

// Un·e gestionnaire de groupe de participant·e·s n'a pas accès à la query `projects`.
const resolvers = {
  Query: {
    projects: () => {
      throw new Error("Unauthorized");
    },
    organizations: () => [organization]
  }
};

const AppShellStub = {
  name: "AppShell",
  props: ["loading"],
  setup(props, { slots }) {
    return () => h("div", { class: "app-shell" }, [slots.title ? slots.title() : null, props.loading ? null : slots.default?.()]);
  }
};

const TitleStub = {
  name: "Title",
  props: ["title"],
  setup(props, { slots }) {
    return () => h("div", [h("h1", props.title), slots.subpagesCta ? slots.subpagesCta() : null]);
  }
};

const RouterViewStub = {
  name: "RouterView",
  setup(props, { slots }) {
    return () => h("div", slots.default ? slots.default({ Component: null }) : null);
  }
};

function createMountOptions() {
  const apolloClient = createTestingApollo({ typeDefs, resolvers });
  const pinia = createTestingPinia({ createSpy: jest.fn, stubActions: false });
  useAuthStore().initialize({ [CLAIM_UTYPE]: CLAIM_UTYPE_ORGANIZATIONMANAGER }, [GLOBAL_MANAGE_TRANSACTIONS]);

  return {
    global: {
      plugins: [i18n, pinia, ConstsPlugin],
      provide: {
        [DefaultApolloClient]: apolloClient
      },
      stubs: {
        RouterView: RouterViewStub,
        AppShell: AppShellStub,
        Title: TitleStub,
        ReportFilters: true,
        UiEmptyPage: true,
        UiCta: true,
        UiPagination: true,
        UiTable: true,
        UiTableNoScroll: true
      }
    }
  };
}

describe("BudgetAllowanceReport.vue", () => {
  beforeEach(() => {
    i18n.global.locale.value = "fr";
  });

  // CRCL-2684 : le rapport était offert aux gestionnaires de groupe de participant·e·s (CRCL-2408),
  // mais seul le chemin programme existait, d'où une page vide.
  it("shows the report of the organization managed by the current user", async () => {
    const wrapper = mount(BudgetAllowanceReport, createMountOptions());

    await flushPromises();

    expect(wrapper.findComponent(BudgetAllowanceReportTable).exists()).toBe(true);
  });
});

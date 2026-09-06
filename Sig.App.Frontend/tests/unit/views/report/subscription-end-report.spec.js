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

import SubscriptionEndReport from "@/views/report/SubscriptionEndReport.vue";
import SubscriptionEndReportTable from "@/components/report/subscription-end-report-table";

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
  type SubscriptionEndReportTotal {
    totalPurchases: Float
    cardsWithFunds: Float
    cardsUsedForPurchases: Float
    merchantsWithPurchases: Float
    totalFundsLoaded: Float
    totalPurchaseValue: Float
    totalExpiredAmount: Float
  }
  type SubscriptionEndTransaction {
    subscription: Subscription
    totalPurchases: Float
    cardsWithFunds: Float
    cardsUsedForPurchases: Float
    merchantsWithPurchases: Float
    totalFundsLoaded: Float
    totalPurchaseValue: Float
    totalExpiredAmount: Float
  }
  type SubscriptionEndReportItem {
    organization: Organization
    subscriptionEndTransactions: [SubscriptionEndTransaction!]!
  }
  type SubscriptionEndReport {
    total: SubscriptionEndReportTotal
    totalCount: Int!
    totalPages: Int!
    items: [SubscriptionEndReportItem!]!
  }
  type Organization {
    id: ID!
    name: String!
    budgetAllowances: [BudgetAllowance!]!
    subscriptionEndReport(
      page: Int!
      limit: Int!
      startDate: DateTime!
      endDate: DateTime!
      withSpecificSubscriptions: [ID!]
    ): SubscriptionEndReport
  }
  type Project {
    id: ID!
    name: String
    reconciliationReportDate: String
    organizations: [Organization!]!
    subscriptions: [Subscription!]!
    subscriptionEndReport(
      page: Int!
      limit: Int!
      startDate: DateTime!
      endDate: DateTime!
      withSpecificOrganizations: [ID!]
      withSpecificSubscriptions: [ID!]
    ): SubscriptionEndReport
  }
  type Query {
    projects: [Project!]!
    organizations: [Organization!]!
  }
`;

const subscription = { __typename: "Subscription", id: "sub-1", name: "Feeding Futures 2026", isArchived: false };
const organizationInfo = { __typename: "Organization", id: "org-1", name: "Penticton SD67" };

const subscriptionEndReport = {
  __typename: "SubscriptionEndReport",
  total: {
    __typename: "SubscriptionEndReportTotal",
    totalPurchases: 210,
    cardsWithFunds: 0,
    cardsUsedForPurchases: 90,
    merchantsWithPurchases: 26,
    totalFundsLoaded: 0,
    totalPurchaseValue: 3222.8,
    totalExpiredAmount: 0
  },
  totalCount: 1,
  totalPages: 1,
  items: [
    {
      __typename: "SubscriptionEndReportItem",
      organization: organizationInfo,
      subscriptionEndTransactions: [
        {
          __typename: "SubscriptionEndTransaction",
          subscription,
          totalPurchases: 210,
          cardsWithFunds: 0,
          cardsUsedForPurchases: 90,
          merchantsWithPurchases: 26,
          totalFundsLoaded: 0,
          totalPurchaseValue: 3222.8,
          totalExpiredAmount: 0
        }
      ]
    }
  ]
};

const organization = {
  __typename: "Organization",
  id: "org-1",
  name: "Penticton SD67",
  budgetAllowances: [{ __typename: "BudgetAllowance", id: "ba-1", subscription }],
  subscriptionEndReport
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
    return () =>
      h("div", { class: "app-shell", "data-loading": String(props.loading) }, [
        slots.title ? slots.title() : null,
        props.loading ? null : slots.default?.()
      ]);
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
  loginAsOrganizationManager();

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
        UiTable: true
      }
    }
  };
}

function loginAsOrganizationManager() {
  const authStore = useAuthStore();
  authStore.initialize({ [CLAIM_UTYPE]: CLAIM_UTYPE_ORGANIZATIONMANAGER }, [GLOBAL_MANAGE_TRANSACTIONS]);
}

describe("SubscriptionEndReport.vue", () => {
  beforeEach(() => {
    i18n.global.locale.value = "fr";
  });

  // CRCL-2684 : le rapport doit se charger pour un·e gestionnaire de groupe de participant·e·s,
  // qui interroge `organizations` plutôt que `projects`.
  it("shows the report of the organization managed by the current user", async () => {
    const wrapper = mount(SubscriptionEndReport, createMountOptions());

    await flushPromises();

    expect(wrapper.findComponent(SubscriptionEndReportTable).exists()).toBe(true);
  });
});

import { mount } from "@vue/test-utils";

import i18n from "@/lib/i18n";

import BudgetAllowanceReportTable from "@/components/report/budget-allowance-report-table";
import UiTableNoScroll from "@/components/ui/table-no-scroll/index.vue";

const AMOUNT_COLUMN = 3;

function moveLog(overrides) {
  return {
    id: "log-1",
    discriminator: "MOVE_BUDGET_ALLOWANCE_LOG",
    createdAt: "2026-08-15T12:00:00Z",
    amount: 300,
    organizationName: "Groupe source",
    subscriptionName: "Feeding Futures 2026",
    targetOrganizationName: "Groupe destinataire",
    targetSubscriptionName: "Feeding Futures 2026",
    isIncomingTransfer: false,
    ...overrides
  };
}

function amountOf(budgetAllowanceLog) {
  const wrapper = mount(BudgetAllowanceReportTable, {
    props: { budgetAllowanceLogs: [budgetAllowanceLog] },
    global: {
      plugins: [i18n],
      components: { UiTableNoScroll }
    }
  });

  return wrapper.findAll("tbody td")[AMOUNT_COLUMN].text();
}

describe("budget-allowance-report-table.vue", () => {
  beforeEach(() => {
    i18n.global.locale.value = "fr";
  });

  it("shows a transfer as a withdrawal for the organization that sent it", () => {
    expect(amountOf(moveLog())).toBe("-300,00$");
  });

  // CRCL-2684 : le rapport de groupe inclut les transferts reçus. Le tableau inversait le signe de
  // tout transfert, si bien qu'une enveloppe qui augmentait s'affichait en négatif.
  it("shows a transfer as an addition for the organization that received it", () => {
    expect(amountOf(moveLog({ isIncomingTransfer: true }))).toBe("+300,00$");
  });

  it("still shows a deletion as a withdrawal", () => {
    expect(amountOf(moveLog({ discriminator: "DELETE_BUDGET_ALLOWANCE_LOG" }))).toBe("-300,00$");
  });

  it("still shows a creation as an addition", () => {
    expect(amountOf(moveLog({ discriminator: "CREATE_BUDGET_ALLOWANCE_LOG" }))).toBe("+300,00$");
  });
});

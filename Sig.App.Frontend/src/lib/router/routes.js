import * as urls from "@/lib/consts/urls";
import Home from "@/views/Home.vue";
import NotFound from "@/views/NotFound.vue";
import {
  GLOBAL_MANAGE_ALL_USERS,
  GLOBAL_MANAGE_ALL_PROJECTS,
  GLOBAL_MANAGE_ALL_MARKETS,
  GLOBAL_MANAGE_ORGANIZATIONS,
  GLOBAL_MANAGE_ORGANIZATION_MANAGERS,
  GLOBAL_MANAGE_BENEFICIARIES,
  GLOBAL_MANAGE_PROJECT_MANAGERS,
  GLOBAL_MANAGE_SUBSCRIPTIONS,
  GLOBAL_MANAGE_CATEGORIES,
  GLOBAL_MANAGE_CARDS,
  GLOBAL_CREATE_TRANSACTION,
  GLOBAL_MANAGE_TRANSACTIONS,
  GLOBAL_MANAGE_PRODUCT_GROUP,
  GLOBAL_REFUND_TRANSACTION
} from "@/lib/consts/permissions";

import { useAuthStore } from "@/lib/store/auth";

const anonymous = true;
const fullscreen = true;
const notConnected = true;

/*
meta: {
  anonymous: false,   // If true, does not require login
  fullscreen: false,  // If true, hides main app navigation
  notConnected: false // If true, the user need to be not connected to use this page
  userType: "",       // If set, validate if current user is of the specific type (use USER_TYPE enums)
  claim: ""           // If set, validate if current user have specific claims (use PERMISSIONS enums)
}
*/

export default [
  {
    path: "/",
    name: urls.URL_ROOT,
    component: Home
  },
  {
    path: "/login",
    name: urls.URL_ACCOUNT_LOGIN,
    component: () => import("@/views/account/Login.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    name: urls.URL_ACCOUNT_CREATE,
    path: "/subscribe",
    component: () => import("@/views/account/Create.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    name: urls.URL_ACCOUNT_ADMIN_CONFIRM,
    path: "/registration/admin",
    component: () => import("@/views/account/ConfirmAdmin.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    name: urls.URL_ACCOUNT_PROJECT_MANAGER_CONFIRM,
    path: "/registration/project-manager",
    component: () => import("@/views/account/ConfirmProjectManager.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    name: urls.URL_ACCOUNT_MERCHANT_CONFIRM,
    path: "/registration/merchant",
    component: () => import("@/views/account/ConfirmMerchant.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    name: urls.URL_ACCOUNT_ORGANIZATION_MANAGER_CONFIRM,
    path: "/registration/organization-manager",
    component: () => import("@/views/account/ConfirmOrganizationManager.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    key: urls.URL_ACCOUNT_CONFIRM,
    path: "/confirm-email",
    component: () => import("@/views/account/Confirm.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    name: urls.URL_ACCOUNT_FORGOT_PASSWORD,
    path: "/forgot-password",
    component: () => import("@/views/account/ForgotPassword.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    name: urls.URL_ACCOUNT_RESET_PASSWORD,
    path: "/reset-password",
    component: () => import("@/views/account/ResetPassword.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    name: urls.URL_PROFILE_EDIT,
    path: "/edit-profile",
    component: () => import("@/views/profile/Edit.vue")
  },
  {
    name: urls.URL_ACCOUNT_SETTINGS,
    path: "/settings",
    component: () => import("@/views/account/Settings.vue")
  },
  {
    name: urls.URL_ACCOUNT_CONFIRM_EMAIL_CHANGE,
    path: "/confirm-change-email",
    component: () => import("@/views/account/ConfirmEmailChange.vue")
  },
  {
    name: urls.URL_ADMIN_USERS,
    path: "/users",
    component: () => import("@/views/admin/ListUsers.vue"),
    meta: {
      claim: GLOBAL_MANAGE_ALL_USERS
    },
    children: [
      {
        name: urls.URL_ADMIN_ADD_USER,
        path: "/users/add",
        component: () => import("@/views/admin/AddAdmin.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_USERS
        }
      },
      {
        name: urls.URL_ADMIN_USER_PROFILE,
        path: "/users/edit/:id",
        component: () => import("@/views/admin/EditUser.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_USERS
        }
      }
    ]
  },
  {
    name: urls.URL_PROJECT_ADMIN_DASHBOARD,
    path: "/dashboard",
    component: () => import("@/views/dashboard/Dashboard.vue"),
    meta: {
      claim: GLOBAL_MANAGE_ORGANIZATIONS
    }
  },
  {
    name: urls.URL_PROJECT_ADMIN,
    path: "/programs",
    component: () => import("@/views/project/ListProjects.vue"),
    meta: {
      claim: GLOBAL_MANAGE_ALL_PROJECTS
    },
    children: [
      {
        name: urls.URL_PROJECT_MANAGE_MERCHANTS,
        path: ":projectId/manage-merchants",
        component: () => import("@/views/project/EditAssociatedMerchants.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_PROJECTS
        },
        children: [
          {
            name: urls.URL_REMOVE_MERCHANTS_FROM_PROJECT,
            path: ":marketId/remove",
            component: () => import("@/views/project/RemoveMarketFromProject.vue"),
            meta: {
              claim: GLOBAL_MANAGE_ALL_PROJECTS
            }
          }
        ]
      },
      {
        name: urls.URL_PROJECT_MANAGE_MANAGERS,
        path: ":projectId/manage-managers",
        component: () => import("@/views/project/EditManagers.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_PROJECTS
        }
      },
      {
        name: urls.URL_PROJECT_ADD,
        path: "add",
        component: () => import("@/views/project/AddProject.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_PROJECTS
        }
      },
      {
        name: urls.URL_PROJECT_DELETE,
        path: ":projectId/delete",
        component: () => import("@/views/project/DeleteProject.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_PROJECTS
        }
      },
      {
        name: urls.URL_PROJECT_EDIT,
        path: ":projectId/edit",
        component: () => import("@/views/project/EditProject.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_PROJECTS
        }
      }
    ]
  },
  {
    name: urls.URL_MARKET_ADMIN,
    path: "/markets",
    component: () => import("@/views/market/ListMarkets.vue"),
    meta: {
      claim: GLOBAL_MANAGE_ALL_MARKETS
    },
    children: [
      {
        name: urls.URL_MARKET_ADD,
        path: "add",
        component: () => import("@/views/market/AddMarket.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_MARKETS
        }
      },
      {
        name: urls.URL_MARKET_EDIT,
        path: ":marketId/edit",
        component: () => import("@/views/market/EditMarket.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_MARKETS
        }
      },
      {
        name: urls.URL_MARKET_DELETE,
        path: ":marketId/delete",
        component: () => import("@/views/market/DeleteMarket.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_MARKETS
        }
      },
      {
        name: urls.URL_MARKET_ARCHIVE,
        path: ":marketId/archive",
        component: () => import("@/views/market/ArchiveMarket.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_MARKETS
        }
      },
      {
        name: urls.URL_MARKET_MANAGE_MANAGERS,
        path: ":marketId/manage-managers",
        component: () => import("@/views/market/EditManagers.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ALL_MARKETS
        }
      }
    ]
  },
  {
    name: urls.URL_TRANSACTION_ADMIN,
    path: "/transactions",
    component: () => import("@/views/transaction/ListTransactions.vue"),
    meta: {
      claim: GLOBAL_MANAGE_TRANSACTIONS
    },
    children: [
      {
        name: urls.URL_TRANSACTION_ADD,
        path: "add",
        component: () => import("@/views/transaction/AddTransaction.vue"),
        meta: {
          claim: GLOBAL_CREATE_TRANSACTION
        }
      },
      {
        name: urls.URL_TRANSACTION_ADMIN_REFUND,
        path: ":transactionId/refund",
        component: () => import("@/views/transaction/Refund.vue"),
        meta: {
          claim: GLOBAL_REFUND_TRANSACTION
        }
      }
    ]
  },
  {
    name: urls.URL_MARKET_OVERVIEW,
    path: "/markets-overview",
    component: () => import("@/views/market/ViewMarkets.vue"),
    meta: {
      claim: GLOBAL_MANAGE_CARDS
    }
  },
  {
    name: urls.URL_ORGANIZATION_ADMIN,
    path: "/organizations",
    component: () => import("@/views/organization/ListOrganizations.vue"),
    meta: {
      claim: GLOBAL_MANAGE_ORGANIZATIONS
    },
    children: [
      {
        name: urls.URL_ORGANIZATION_ADD,
        path: "add",
        component: () => import("@/views/organization/AddOrganization.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ORGANIZATIONS
        }
      },
      {
        name: urls.URL_ORGANIZATION_DELETE,
        path: ":organizationId/delete",
        component: () => import("@/views/organization/DeleteOrganization.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ORGANIZATIONS
        }
      },
      {
        name: urls.URL_ORGANIZATION_MANAGE_MANAGERS,
        path: ":organizationId/manage-managers",
        component: () => import("@/views/organization/EditManagers.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ORGANIZATIONS
        }
      },
      {
        name: urls.URL_ORGANIZATION_EDIT,
        path: ":organizationId/edit",
        component: () => import("@/views/organization/EditOrganization.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ORGANIZATIONS
        }
      }
    ]
  },
  {
    name: urls.URL_PROJECT_MANAGER_ADMIN,
    path: "/project-managers",
    component: () => import("@/views/project/ListProjectManagers.vue"),
    meta: {
      claim: GLOBAL_MANAGE_PROJECT_MANAGERS
    },
    children: [
      {
        name: urls.URL_PROJECT_MANAGER_ADD,
        path: "add",
        component: () => import("@/views/project/AddProjectManager.vue"),
        meta: {
          claim: GLOBAL_MANAGE_PROJECT_MANAGERS
        }
      },
      {
        name: urls.URL_PROJECT_MANAGER_REMOVE,
        path: ":managerId/remove",
        component: () => import("@/views/project/RemoveProjectManager.vue"),
        meta: {
          claim: GLOBAL_MANAGE_PROJECT_MANAGERS
        }
      }
    ]
  },
  {
    name: urls.URL_ORGANIZATION_MANAGER_ADMIN,
    path: "/organization-managers",
    component: () => import("@/views/organization/ListOrganizationManagers.vue"),
    meta: {
      claim: GLOBAL_MANAGE_ORGANIZATION_MANAGERS
    },
    children: [
      {
        name: urls.URL_ORGANIZATION_MANAGER_ADD,
        path: "add",
        component: () => import("@/views/organization/AddOrganizationManager.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ORGANIZATION_MANAGERS
        }
      },
      {
        name: urls.URL_ORGANIZATION_MANAGER_REMOVE,
        path: ":managerId/remove",
        component: () => import("@/views/organization/RemoveProjectManager.vue"),
        meta: {
          claim: GLOBAL_MANAGE_ORGANIZATION_MANAGERS
        }
      }
    ]
  },
  {
    name: urls.URL_SUBSCRIPTION_ADMIN,
    path: "/subscriptions",
    component: () => import("@/views/subscription/ListSubscriptions.vue"),
    meta: {
      claim: GLOBAL_MANAGE_SUBSCRIPTIONS
    },
    children: [
      {
        name: urls.URL_SUBSCRIPTION_ADD,
        path: "add",
        component: () => import("@/views/subscription/AddSubscription.vue"),
        meta: {
          claim: GLOBAL_MANAGE_SUBSCRIPTIONS
        }
      },
      {
        name: urls.URL_SUBSCRIPTION_EDIT,
        path: ":subscriptionId/edit",
        component: () => import("@/views/subscription/EditSubscription.vue"),
        meta: {
          claim: GLOBAL_MANAGE_SUBSCRIPTIONS
        }
      },
      {
        name: urls.URL_SUBSCRIPTION_DELETE,
        path: ":subscriptionId/delete",
        component: () => import("@/views/subscription/DeleteSubscription.vue"),
        meta: {
          claim: GLOBAL_MANAGE_SUBSCRIPTIONS
        }
      },
      {
        name: urls.URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE,
        path: ":subscriptionId/budgets",
        component: () => import("@/views/budget-allowance/EditBudgetAllowance.vue"),
        meta: {
          claim: GLOBAL_MANAGE_SUBSCRIPTIONS
        }
      },
      {
        name: urls.URL_SUBSCRIPTION_DELETE_BUDGET_ALLOWANCE,
        path: ":subscriptionId/budgets/:budgetId/delete",
        component: () => import("@/views/budget-allowance/DeleteBudgetAllowance.vue"),
        meta: {
          claim: GLOBAL_MANAGE_SUBSCRIPTIONS
        }
      }
    ]
  },
  {
    name: urls.URL_BENEFICIARY_ADMIN,
    path: "/participants",
    component: () => import("@/views/beneficiary/ListBeneficiaries.vue"),
    meta: {
      claim: GLOBAL_MANAGE_BENEFICIARIES
    },
    children: [
      {
        name: urls.URL_BENEFICIARY_CARD_ASSIGN,
        path: ":beneficiaryId/assign",
        component: () => import("@/views/card/AssignCard.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      },
      {
        name: urls.URL_BENEFICIARY_CARD_UNASSIGN,
        path: ":beneficiaryId/unassign/:cardId",
        component: () => import("@/views/card/UnassignCard.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      },
      {
        name: urls.URL_BENEFICIARY_CARD_LOST,
        path: ":beneficiaryId/lost-card/:cardId",
        component: () => import("@/views/card/TransferCard.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      },
      {
        name: urls.URL_BENEFICIARY_QRCODE_PREVIEW,
        path: ":cardId/preview",
        component: () => import("@/views/card/PreviewQRCode.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      },
      {
        name: urls.URL_BENEFICIARY_REMOVE_SUBSCRIPTION,
        path: ":beneficiaryId/:subscriptionId/remove",
        component: () => import("@/views/beneficiary/RemoveSubscriptionFromBeneficiary.vue"),
        meta: {
          claim: GLOBAL_MANAGE_BENEFICIARIES
        }
      },
      {
        name: urls.URL_BENEFICIARY_MANAGE_SUBSCRIPTIONS,
        path: ":beneficiaryId/manage-subscriptions",
        component: () => import("@/views/beneficiary/EditAssociatedSubscriptions.vue"),
        meta: {
          claim: GLOBAL_MANAGE_BENEFICIARIES
        }
      },
      {
        name: urls.URL_BENEFICIARY_ADD,
        path: "add",
        component: () => import("@/views/beneficiary/AddBeneficiary.vue"),
        meta: {
          claim: GLOBAL_MANAGE_BENEFICIARIES
        }
      },
      {
        name: urls.URL_BENEFICIARY_EDIT,
        path: ":beneficiaryId/edit",
        component: () => import("@/views/beneficiary/EditBeneficiary.vue"),
        meta: {
          claim: GLOBAL_MANAGE_BENEFICIARIES
        }
      },
      {
        name: urls.URL_BENEFICIARY_DELETE,
        path: ":beneficiaryId/delete",
        component: () => import("@/views/beneficiary/DeleteBeneficiary.vue"),
        meta: {
          claim: GLOBAL_MANAGE_BENEFICIARIES
        }
      },
      {
        name: urls.URL_BENEFICIARY_MANUALLY_ADD_FUND,
        path: ":beneficiaryId/add-fund",
        component: () => import("@/views/beneficiary/ManuallyAddFund.vue"),
        meta: {
          claim: GLOBAL_MANAGE_BENEFICIARIES
        }
      },
      {
        name: urls.URL_BENEFICIARY_IMPORT_LIST,
        path: "import",
        component: () => import("@/views/beneficiary/ImportList.vue"),
        meta: {
          claim: GLOBAL_MANAGE_BENEFICIARIES
        }
      },
      {
        name: urls.URL_BENEFICIARY_OFF_PLATFORM_IMPORT_LIST,
        path: "import-off-platform",
        component: () => import("@/views/beneficiary/OffPlatformImportList.vue"),
        meta: {
          claim: GLOBAL_MANAGE_BENEFICIARIES
        }
      }
    ]
  },
  {
    name: urls.URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS,
    path: "/assign-subscriptions",
    component: () => import("@/views/beneficiary/AssignSubscriptions.vue"),
    meta: {
      claim: GLOBAL_MANAGE_BENEFICIARIES
    }
  },
  {
    name: urls.URL_CARDS,
    path: "/cards",
    component: () => import("@/views/card/_index.vue"),
    meta: {
      claim: GLOBAL_MANAGE_CARDS
    },
    children: [
      {
        name: urls.URL_CARDS_QRCODE_PREVIEW,
        path: ":cardId/preview",
        component: () => import("@/views/card/PreviewQRCode.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      },
      {
        name: urls.URL_CARDS_UNASSIGN,
        path: ":beneficiaryId/unassign/:cardId",
        component: () => import("@/views/card/UnassignCard.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      },
      {
        name: urls.URL_CARDS_LOST,
        path: ":beneficiaryId/lost-card/:cardId",
        component: () => import("@/views/card/TransferCard.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      },
      {
        name: urls.URL_CARDS_ADD,
        path: "add",
        component: () => import("@/views/card/AddCards.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      },
      {
        name: urls.URL_GIFT_CARD_ADD,
        path: "add-gift-card",
        component: () => import("@/views/card/AddGiftCard.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CARDS
        }
      }
    ]
  },
  {
    name: urls.URL_CARDS_BRANDING,
    path: "/card-branding",
    component: () => import("@/views/card/CardBranding.vue"),
    meta: {
      claim: GLOBAL_MANAGE_CARDS
    }
  },
  {
    name: urls.URL_TRANSACTION,
    path: "/transaction",
    component: () => import("@/views/transaction/Transaction.vue"),
    meta: {
      claim: GLOBAL_CREATE_TRANSACTION
    },
    children: [
      {
        name: urls.URL_TRANSACTION_ERROR,
        path: "error",
        component: () => import("@/views/transaction/Error.vue"),
        meta: {
          claim: GLOBAL_CREATE_TRANSACTION
        }
      }
    ]
  },
  {
    name: urls.URL_TRANSACTION_LIST,
    path: "/market-transactions",
    component: () => import("@/views/transaction/MarketListTransaction.vue"),
    meta: {
      claim: GLOBAL_CREATE_TRANSACTION
    },
    children: [
      {
        name: urls.URL_TRANSACTION_REFUND,
        path: ":transactionId/refund",
        component: () => import("@/views/transaction/Refund.vue"),
        meta: {
          claim: GLOBAL_CREATE_TRANSACTION
        }
      }
    ]
  },
  {
    name: urls.URL_CARD_CHECK,
    path: "/check",
    component: () => import("@/views/card/CardCheck.vue"),
    meta: {
      anonymous
    },
    children: [
      {
        name: urls.URL_CARD_ERROR,
        path: "error",
        component: () => import("@/views/card/Error.vue"),
        meta: {
          anonymous
        }
      }
    ]
  },
  {
    name: urls.URL_PROJECT_SETTINGS,
    path: "/programs-settings",
    component: () => import("@/views/project-settings/_index.vue"),
    redirect: () => {
      const auth = useAuthStore();
      if (auth.getGlobalPermissions.includes(GLOBAL_MANAGE_CATEGORIES)) {
        return { name: urls.URL_CATEGORY_ADMIN };
      } else {
        return { name: urls.URL_PRODUCT_GROUP_ADMIN };
      }
    },
    children: [
      {
        name: urls.URL_CATEGORY_ADMIN,
        path: "categories",
        component: () => import("@/views/category/ListCategories.vue"),
        meta: {
          claim: GLOBAL_MANAGE_CATEGORIES
        },
        children: [
          {
            name: urls.URL_CATEGORY_ADD,
            path: "add",
            component: () => import("@/views/category/AddCategory.vue"),
            meta: {
              claim: GLOBAL_MANAGE_CATEGORIES
            }
          },
          {
            name: urls.URL_CATEGORY_EDIT,
            path: ":categoryId/edit",
            component: () => import("@/views/category/EditCategory.vue"),
            meta: {
              claim: GLOBAL_MANAGE_CATEGORIES
            }
          },
          {
            name: urls.URL_CATEGORY_DELETE,
            path: ":categoryId/delete",
            component: () => import("@/views/category/DeleteCategory.vue"),
            meta: {
              claim: GLOBAL_MANAGE_CATEGORIES
            }
          }
        ]
      },
      {
        name: urls.URL_PRODUCT_GROUP_ADMIN,
        path: "product-groups",
        component: () => import("@/views/product-groups/ListProductGroups.vue"),
        meta: {
          claim: GLOBAL_MANAGE_PRODUCT_GROUP
        },
        children: [
          {
            name: urls.URL_PRODUCT_GROUP_ADD,
            path: "add",
            component: () => import("@/views/product-groups/AddProductGroup.vue"),
            meta: {
              claim: GLOBAL_MANAGE_PRODUCT_GROUP
            }
          },
          {
            name: urls.URL_PRODUCT_GROUP_EDIT,
            path: ":productGroupId/edit",
            component: () => import("@/views/product-groups/EditProductGroup.vue"),
            meta: {
              claim: GLOBAL_MANAGE_PRODUCT_GROUP
            }
          },
          {
            name: urls.URL_PRODUCT_GROUP_DELETE,
            path: ":productGroupId/delete",
            component: () => import("@/views/product-groups/DeleteProductGroup.vue"),
            meta: {
              claim: GLOBAL_MANAGE_PRODUCT_GROUP
            }
          }
        ]
      }
    ]
  },
  {
    path: "/doc/form-example",
    name: urls.URL_DOC_FORM,
    component: () => import("@/views/doc/FormExample.vue"),
    meta: {
      fullscreen,
      anonymous,
      notConnected
    }
  },
  {
    path: "/doc/calendar",
    name: urls.URL_DOC_CALENDAR,
    component: () => import("@/views/doc/Calendar.vue")
  },
  {
    path: "/doc/btn-group",
    name: urls.URL_DOC_BTN_GROUP,
    component: () => import("@/views/doc/BtnGroup.vue")
  },
  {
    path: "/doc/dialog",
    name: urls.URL_DOC_DIALOG,
    component: () => import("@/views/doc/Dialog.vue"),
    children: [
      {
        path: "basic",
        name: urls.URL_DOC_DIALOG_BASIC,
        component: () => import("@/views/doc/dialog/BasicDialog.vue")
      },
      {
        path: "confirm",
        name: urls.URL_DOC_DIALOG_CONFIRM,
        component: () => import("@/views/doc/dialog/ConfirmDialog.vue")
      },
      {
        path: "delete",
        name: urls.URL_DOC_DIALOG_DELETE,
        component: () => import("@/views/doc/dialog/DeleteDialog.vue")
      }
    ]
  },
  {
    path: "/:pathMatch(.*)*",
    component: NotFound,
    meta: {
      fullscreen,
      anonymous
    }
  }
];

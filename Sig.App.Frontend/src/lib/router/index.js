import { createRouter, createWebHistory } from "vue-router";

import {
  URL_ACCOUNT_LOGIN,
  URL_PROJECT_ADMIN,
  URL_PROJECT_ADMIN_DASHBOARD,
  URL_BENEFICIARY_ADMIN,
  URL_TRANSACTION,
  URL_RECONCILIATION_REPORT,
  URL_ROOT
} from "@/lib/consts/urls";
import {
  USER_TYPE_MERCHANT,
  USER_TYPE_ORGANIZATIONMANAGER,
  USER_TYPE_PROJECTMANAGER,
  USER_TYPE_PCAADMIN,
  USER_TYPE_MARKETGROUPMANAGER
} from "@/lib/consts/enums";

import routes from "./routes";
import { useAuthStore } from "@/lib/store/auth";

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition;
    }

    if (to.hash) {
      return { el: to.hash };
    }

    return { top: 0 };
  }
});

router.beforeEach(async (to, from, next) => {
  const auth = useAuthStore();

  // Enforce logged-in user except for anonymous routes
  if (to.matched.some((r) => !r.meta.anonymous)) {
    await waitUntilAuthIsInitialized();

    if (!auth.isLoggedIn) {
      const query = {};

      if (to.fullPath !== "/") {
        query.returnPath = to.fullPath;
      }

      return next({
        name: URL_ACCOUNT_LOGIN,
        query
      });
    }
  }
  // Enforce not connected user for specific routes
  else if (to.matched.some((r) => r.meta.notConnected)) {
    await waitUntilAuthIsInitialized();

    if (auth.isLoggedIn) {
      return next({
        name: URL_ROOT
      });
    }
  }

  if (to.name === URL_ROOT) {
    const userType = auth.userType;
    const name =
      userType === USER_TYPE_PCAADMIN
        ? URL_PROJECT_ADMIN
        : userType === USER_TYPE_PROJECTMANAGER
        ? URL_PROJECT_ADMIN_DASHBOARD
        : userType === USER_TYPE_ORGANIZATIONMANAGER
        ? URL_BENEFICIARY_ADMIN
        : userType === USER_TYPE_MERCHANT
        ? URL_TRANSACTION
        : userType === USER_TYPE_MARKETGROUPMANAGER
        ? URL_RECONCILIATION_REPORT
        : URL_ROOT;

    return next({
      name
    });
  }

  // Enforce usertype constraints
  for (const match of to.matched) {
    if (match.meta.usertype) {
      if (Array.isArray(match.meta.usertype)) {
        if (!match.meta.usertype.includes(auth.userType)) {
          return next({
            name: URL_ROOT
          });
        }
      } else if (auth.userType !== match.meta.usertype) {
        return next({
          name: URL_ROOT
        });
      }
    }
  }

  // Enfore claims constraints
  for (const match of to.matched) {
    if (match.meta.claim) {
      if (!auth.getGlobalPermissions.includes(match.meta.claim)) {
        return next({
          name: URL_ROOT
        });
      }
    }
  }

  next();
});

function waitUntilAuthIsInitialized() {
  const authStore = useAuthStore();

  return new Promise((resolve) => {
    if (authStore.initialized) resolve();

    const unsubscribe = authStore.$onAction(({ store, after }) => {
      after(() => {
        if (store.initialized) {
          resolve();
          unsubscribe();
        }
      });
    });
  });
}

export default router;

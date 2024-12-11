/* eslint-disable @intlify/vue-i18n/no-missing-keys */
import { AccountLoginBadCredentials, AccountLoginUnconfirmed, AccountLoginDisabled } from "@/lib/consts/problems";
import { URL_ROOT } from "@/lib/consts/urls";
import {
  CLAIM_UTYPE_PROJECTMANAGER_OF,
  CLAIM_UTYPE_MARKETMANAGER_OF,
  CLAIM_UTYPE_ORGANIZATIONMANAGER_OF,
  CLAIM_UTYPE,
  CLAIM_UTYPE_PROJECTMANAGER,
  CLAIM_UTYPE_ORGANIZATIONMANAGER,
  CLAIM_UTYPE_MERCHANT
} from "@/lib/consts/claims";
import {
  ProjectManagerWithoutProjectError,
  MarketManagerWithoutMarketError,
  OrganizationManagerWithoutOrganizationError
} from "@/lib/consts/problems";

import router from "@/lib/router";
import i18n from "@/lib/i18n";

import { Client } from "@/lib/helpers/client";
import { useNotificationsStore } from "@/lib/store/notifications";
import { useAuthStore } from "@/lib/store/auth";

async function login(username, password) {
  const { addError } = useNotificationsStore();

  const requestData = {
    username,
    password
  };

  let response = null;
  try {
    response = await Client.post("/account/login", JSON.stringify(requestData));
  } catch (error) {
    switch (error?.response?.data?.type) {
      case AccountLoginBadCredentials:
        addError(i18n.global.t("authentication-login-bad-credentials"));
        break;
      case AccountLoginUnconfirmed:
        addError(i18n.global.t("authentication-login-unconfirmed"));
        break;
      case AccountLoginDisabled:
        addError(i18n.global.t("authentication-login-disabled"));
        break;
      default:
        addError(i18n.global.t("authentication-login-error"));
    }

    return;
  }

  const claims = response.data.claims;
  const globalPermissions = response.data.globalPermissions;

  if (claims[CLAIM_UTYPE] === CLAIM_UTYPE_PROJECTMANAGER) {
    if (claims[CLAIM_UTYPE_PROJECTMANAGER_OF] === undefined) {
      logout(ProjectManagerWithoutProjectError);
      return;
    }
  } else if (claims[CLAIM_UTYPE] === CLAIM_UTYPE_ORGANIZATIONMANAGER) {
    if (claims[CLAIM_UTYPE_ORGANIZATIONMANAGER_OF] === undefined) {
      logout(OrganizationManagerWithoutOrganizationError);
      return;
    }
  } else if (claims[CLAIM_UTYPE] === CLAIM_UTYPE_MERCHANT) {
    if (claims[CLAIM_UTYPE_MARKETMANAGER_OF] === undefined) {
      logout(MarketManagerWithoutMarketError);
      return;
    }
  }

  const { initialize } = useAuthStore();

  initialize(claims, globalPermissions);

  localStorage.removeItem("currentOrganization");

  const path = router.currentRoute.value.query.returnPath || { name: URL_ROOT };
  router.push(path);
}

async function logout(error, returnPath) {
  await Client.post("/account/logout");

  const { initialize } = useAuthStore();
  initialize(null, []);

  localStorage.removeItem("currentOrganization");
  localStorage.removeItem("currentCashRegister");
  localStorage.removeItem("currentMarket");

  var path = "";

  if (error || returnPath) {
    const params = [];

    if (error) {
      params.push("error=" + error);
    }

    if (returnPath) {
      params.push("returnPath=" + returnPath);
    }

    path = "?" + params.join("&");
  }

  window.location.assign("/login" + path);
}

async function refresh() {
  const auth = useAuthStore();
  const wasLoggedIn = auth.isLoggedIn;

  try {
    const response = await Client.get("/account/refresh");

    if (response.status === 200) {
      auth.initialize(response.data.claims, response.data.globalPermissions);
    } else {
      auth.initialize(null, []);
      if (wasLoggedIn) {
        window.location.assign("/");
      }
    }
  } catch (err) {
    auth.initialize(null, []);
    if (wasLoggedIn) {
      window.location.assign("/");
    }
  }
}

export default {
  login,
  logout,
  refresh
};

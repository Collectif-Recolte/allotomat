import { defineStore } from "pinia";
import {
  USER_TYPE_MERCHANT,
  USER_TYPE_ORGANIZATIONMANAGER,
  USER_TYPE_PROJECTMANAGER,
  USER_TYPE_PCAADMIN,
  USER_TYPE_ANONYMOUS
} from "@/lib/consts/enums";
import {
  CLAIM_UTYPE,
  CLAIM_UTYPE_PROJECTMANAGER,
  CLAIM_UTYPE_ORGANIZATIONMANAGER,
  CLAIM_UTYPE_MERCHANT,
  CLAIM_UTYPE_PCAADMIN
} from "@/lib/consts/claims";

type AuthState = {
  claims: Record<string, string> | null;
  initialized: boolean;
  globalPermissions: Array<string>;
};

export const useAuthStore = defineStore("auth", {
  state: (): AuthState => ({
    claims: null,
    initialized: false,
    globalPermissions: []
  }),
  actions: {
    initialize(claims: Record<string, string> | null, globalPermissions: Array<string>) {
      this.$patch((state) => {
        state.initialized = true;
        state.claims = claims;
        state.globalPermissions = globalPermissions;
      });
    }
  },
  getters: {
    isLoggedIn(state) {
      return !!state.claims;
    },
    userType(state) {
      const utype = state.claims == null ? null : state.claims[CLAIM_UTYPE];

      switch (utype) {
        case CLAIM_UTYPE_PCAADMIN:
          return USER_TYPE_PCAADMIN;
        case CLAIM_UTYPE_PROJECTMANAGER:
          return USER_TYPE_PROJECTMANAGER;
        case CLAIM_UTYPE_ORGANIZATIONMANAGER:
          return USER_TYPE_ORGANIZATIONMANAGER;
        case CLAIM_UTYPE_MERCHANT:
          return USER_TYPE_MERCHANT;
      }

      return USER_TYPE_ANONYMOUS;
    },
    getGlobalPermissions(state) {
      return state.globalPermissions;
    }
  }
});

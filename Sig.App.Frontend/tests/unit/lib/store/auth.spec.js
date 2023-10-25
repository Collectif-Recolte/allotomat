import { setActivePinia, createPinia } from "pinia";
import { useAuthStore } from "@/lib/store/auth";
import { CLAIM_UTYPE, CLAIM_UTYPE_PCAADMIN, CLAIM_UTYPE_PROJECTMANAGER } from "@/lib/consts/claims";
import { USER_TYPE_PCAADMIN, USER_TYPE_ANONYMOUS, USER_TYPE_PROJECTMANAGER } from "@/lib/consts/enums";

describe("store/auth.js", () => {
  const sampleClaims = {
    claim1: "claim1",
    claim2: "claim2",
    claim3: "claim3"
  };

  const sampleGlobalPermissions = [];

  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("stores claims and global permissions", () => {
    const authStore = useAuthStore();

    authStore.initialize(sampleClaims, sampleGlobalPermissions);

    expect(authStore.claims).toEqual(sampleClaims);
    expect(authStore.globalPermissions).toEqual(sampleGlobalPermissions);
  });

  it("is marked as initialized after claims and global permissions are set", () => {
    const authStore = useAuthStore();

    authStore.initialize(sampleClaims, sampleGlobalPermissions);

    expect(authStore.initialized).toBeTruthy();
  });

  it("is logged in if the claims and global permissions are set", () => {
    const authStore = useAuthStore();

    expect(authStore.isLoggedIn).toBeFalsy();

    authStore.claims = sampleClaims;
    authStore.globalPermissions = sampleGlobalPermissions;
    expect(authStore.isLoggedIn).toBeTruthy();
  });

  it("can read user type from claims", () => {
    const authStore = useAuthStore();

    expect(authStore.userType).toEqual(USER_TYPE_ANONYMOUS);

    authStore.claims = { [CLAIM_UTYPE]: CLAIM_UTYPE_PCAADMIN };

    expect(authStore.userType).toEqual(USER_TYPE_PCAADMIN);

    authStore.claims = { [CLAIM_UTYPE]: CLAIM_UTYPE_PROJECTMANAGER };

    expect(authStore.userType).toEqual(USER_TYPE_PROJECTMANAGER);
  });
});

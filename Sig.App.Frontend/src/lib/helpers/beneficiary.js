import { GLOBAL_MANAGE_BENEFICIARIES } from "@/lib/consts/permissions";

import { useAuthStore } from "@/lib/store/auth";

export function canEditBeneficiary() {
  const auth = useAuthStore();
  return auth.getGlobalPermissions.includes(GLOBAL_MANAGE_BENEFICIARIES);
}

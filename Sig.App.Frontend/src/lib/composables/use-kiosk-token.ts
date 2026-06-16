import gql from "graphql-tag";
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useMutation, useQuery, useResult } from "@vue/apollo-composable";

import { URL_KIOSK_HOME } from "@/lib/consts/urls";
import { clearKioskSession, setKioskSession, useKioskSession } from "@/lib/composables/use-kiosk-session";

export function useKioskToken() {
  const route = useRoute();
  const router = useRouter();
  const authError = ref(false);

  const token = computed(() => route.params.token as string);
  const authSession = useKioskSession(token);
  const authToken = computed(() => authSession.value?.accessToken ?? "");
  const isAuthenticated = computed(() => !!authToken.value);

  const { result, loading, refetch } = useQuery(
    gql`
      query CashRegisterByKioskToken($token: String!) {
        cashRegisterByKioskToken(token: $token) {
          isValid
          cashRegisterName
          marketIsDisabled
        }
      }
    `,
    () => ({ token: token.value }),
    () => ({ enabled: !!token.value })
  );

  const kioskInfo = useResult(result, null, (data) => data.cashRegisterByKioskToken);
  const isValid = computed(() => kioskInfo.value?.isValid === true);
  const marketIsDisabled = computed(() => kioskInfo.value?.marketIsDisabled === true);
  const tokenFound = computed(() => !!kioskInfo.value?.cashRegisterName);

  const { mutate: authenticateKiosk, loading: loginLoading } = useMutation(gql`
    mutation AuthenticateKiosk($token: String!, $password: String!) {
      authenticateKiosk(token: $token, password: $password) {
        accessToken
        expiresAt
      }
    }
  `);

  function kioskRoute(name: string) {
    return { name, params: { token: token.value } };
  }

  async function login(password: string) {
    authError.value = false;
    try {
      const result = await authenticateKiosk({ token: token.value, password });
      const payload = result?.data?.authenticateKiosk;
      if (!payload?.accessToken) {
        authError.value = true;
        return false;
      }
      setKioskSession(token.value, payload.accessToken, payload.expiresAt);
      return true;
    } catch {
      authError.value = true;
      return false;
    }
  }

  function logout() {
    if (token.value) {
      clearKioskSession(token.value);
    }
    authError.value = false;
  }

  function handleKioskAuthError() {
    logout();
    router.replace(kioskRoute(URL_KIOSK_HOME));
  }

  return {
    token,
    authToken,
    loading,
    loginLoading,
    kioskInfo,
    isValid,
    marketIsDisabled,
    tokenFound,
    isAuthenticated,
    authError,
    refetch,
    kioskRoute,
    login,
    logout,
    handleKioskAuthError
  };
}

import gql from "graphql-tag";
import { computed } from "vue";
import { useRoute } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";

export function useKioskToken() {
  const route = useRoute();

  const token = computed(() => route.params.token as string);

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

  function kioskRoute(name: string) {
    return { name, params: { token: token.value } };
  }

  return {
    token,
    loading,
    kioskInfo,
    isValid,
    marketIsDisabled,
    tokenFound,
    refetch,
    kioskRoute
  };
}

import gql from "graphql-tag";
import { watchEffect } from "vue";
import type { Ref } from "vue";
import { apolloClient } from "@/lib/graphql/apollo-client";

const VALIDATION_INTERVAL_MS = 10 * 60 * 1000;

const VALIDATE_KIOSK_SESSION = gql`
  query ValidateKioskSession($kioskToken: String!) {
    validateKioskSession(kioskToken: $kioskToken)
  }
`;

async function isKioskSessionValid(kioskToken: string): Promise<boolean> {
  try {
    const { data } = await apolloClient.query({
      query: VALIDATE_KIOSK_SESSION,
      variables: { kioskToken },
      fetchPolicy: "network-only"
    });
    return data?.validateKioskSession === true;
  } catch {
    return false;
  }
}

export function useKioskSessionValidation(authToken: Ref<string>, onInvalid: () => void) {
  watchEffect((onCleanup) => {
    const kioskToken = authToken.value;
    if (!kioskToken) {
      return;
    }

    const validate = async () => {
      if (!(await isKioskSessionValid(kioskToken))) {
        onInvalid();
      }
    };

    void validate();
    const intervalId = setInterval(validate, VALIDATION_INTERVAL_MS);
    onCleanup(() => clearInterval(intervalId));
  });
}

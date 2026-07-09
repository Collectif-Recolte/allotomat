import gql from "graphql-tag";
import { watchEffect } from "vue";
import type { Ref } from "vue";
import { apolloClient } from "@/lib/graphql/apollo-client";
import { KIOSK_ACCESS_INVALID } from "@/lib/consts/qr-code-error";

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
  } catch (error) {
    if ((error as Error).message?.indexOf(KIOSK_ACCESS_INVALID) !== -1) {
      return false;
    }
    throw error;
  }
}

export function useKioskSessionValidation(authToken: Ref<string>, onInvalid: () => void) {
  watchEffect((onCleanup) => {
    const kioskToken = authToken.value;
    if (!kioskToken) {
      return;
    }

    const validate = async () => {
      try {
        if (!(await isKioskSessionValid(kioskToken))) {
          onInvalid();
        }
      } catch {
        // Transient errors are retried on the next interval instead of clearing the session.
      }
    };

    void validate();
    const intervalId = setInterval(validate, VALIDATION_INTERVAL_MS);
    onCleanup(() => clearInterval(intervalId));
  });
}

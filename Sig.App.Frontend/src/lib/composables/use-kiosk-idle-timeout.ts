import { onUnmounted, ref, watch, toValue } from "vue";
import type { MaybeRefOrGetter, Ref } from "vue";

import {
  KIOSK_IDLE_TIMEOUT_MS,
  KIOSK_IDLE_WARNING_TIMEOUT_MS,
  KIOSK_PURCHASE_COMPLETE_TIMEOUT_MS
} from "@/lib/consts/kiosk-timeout";
import type { KioskIdleTimeoutMode } from "@/lib/composables/use-kiosk-shell";

const ACTIVITY_EVENTS = ["pointerdown", "touchstart", "keydown", "scroll"] as const;

type UseKioskIdleTimeoutOptions = {
  mode: MaybeRefOrGetter<KioskIdleTimeoutMode>;
  paused: MaybeRefOrGetter<boolean>;
  onReturnHome: () => void;
  purchaseCompleteSecondsRemaining: Ref<number>;
};

export function useKioskIdleTimeout({
  mode,
  paused,
  onReturnHome,
  purchaseCompleteSecondsRemaining
}: UseKioskIdleTimeoutOptions) {
  const showIdlePrompt = ref(false);
  const warningSecondsRemaining = ref(0);

  let idleTimer: ReturnType<typeof setTimeout> | null = null;
  let warningTimer: ReturnType<typeof setTimeout> | null = null;
  let purchaseCompleteTimer: ReturnType<typeof setTimeout> | null = null;
  let purchaseCompleteCountdownInterval: ReturnType<typeof setInterval> | null = null;
  let warningCountdownInterval: ReturnType<typeof setInterval> | null = null;

  function clearIdleTimer() {
    if (idleTimer !== null) {
      clearTimeout(idleTimer);
      idleTimer = null;
    }
  }

  function clearWarningTimer() {
    if (warningTimer !== null) {
      clearTimeout(warningTimer);
      warningTimer = null;
    }
    if (warningCountdownInterval !== null) {
      clearInterval(warningCountdownInterval);
      warningCountdownInterval = null;
    }
    warningSecondsRemaining.value = 0;
  }

  function clearPurchaseCompleteTimer() {
    if (purchaseCompleteTimer !== null) {
      clearTimeout(purchaseCompleteTimer);
      purchaseCompleteTimer = null;
    }
    if (purchaseCompleteCountdownInterval !== null) {
      clearInterval(purchaseCompleteCountdownInterval);
      purchaseCompleteCountdownInterval = null;
    }
    purchaseCompleteSecondsRemaining.value = 0;
  }

  function clearAllTimers() {
    clearIdleTimer();
    clearWarningTimer();
    clearPurchaseCompleteTimer();
  }

  function hideIdlePrompt() {
    showIdlePrompt.value = false;
    clearWarningTimer();
  }

  function returnHome() {
    clearAllTimers();
    hideIdlePrompt();
    onReturnHome();
  }

  function startIdleTimer() {
    clearIdleTimer();
    idleTimer = setTimeout(() => {
      showIdlePrompt.value = true;
      warningSecondsRemaining.value = Math.ceil(KIOSK_IDLE_WARNING_TIMEOUT_MS / 1000);

      warningCountdownInterval = setInterval(() => {
        warningSecondsRemaining.value = Math.max(0, warningSecondsRemaining.value - 1);
      }, 1000);

      warningTimer = setTimeout(returnHome, KIOSK_IDLE_WARNING_TIMEOUT_MS);
    }, KIOSK_IDLE_TIMEOUT_MS);
  }

  function startPurchaseCompleteTimer() {
    clearPurchaseCompleteTimer();
    purchaseCompleteSecondsRemaining.value = Math.ceil(KIOSK_PURCHASE_COMPLETE_TIMEOUT_MS / 1000);

    purchaseCompleteCountdownInterval = setInterval(() => {
      purchaseCompleteSecondsRemaining.value = Math.max(0, purchaseCompleteSecondsRemaining.value - 1);
    }, 1000);

    purchaseCompleteTimer = setTimeout(returnHome, KIOSK_PURCHASE_COMPLETE_TIMEOUT_MS);
  }

  function resetIdle() {
    if (toValue(mode) !== "idle" || toValue(paused)) {
      return;
    }
    hideIdlePrompt();
    startIdleTimer();
  }

  function onActivity() {
    if (toValue(mode) !== "idle" || toValue(paused)) {
      return;
    }
    resetIdle();
  }

  function attachActivityListeners() {
    for (const event of ACTIVITY_EVENTS) {
      window.addEventListener(event, onActivity, { passive: true });
    }
  }

  function detachActivityListeners() {
    for (const event of ACTIVITY_EVENTS) {
      window.removeEventListener(event, onActivity);
    }
  }

  function restart() {
    clearAllTimers();
    hideIdlePrompt();
    detachActivityListeners();

    const currentMode = toValue(mode);
    const isPaused = toValue(paused);

    if (isPaused || currentMode === "disabled") {
      return;
    }

    if (currentMode === "purchase-complete") {
      startPurchaseCompleteTimer();
      return;
    }

    attachActivityListeners();
    startIdleTimer();
  }

  watch(() => [toValue(mode), toValue(paused)] as const, restart, { immediate: true });

  onUnmounted(() => {
    detachActivityListeners();
    clearAllTimers();
  });

  return {
    showIdlePrompt,
    warningSecondsRemaining,
    resetIdle,
    dismissIdlePrompt: resetIdle
  };
}

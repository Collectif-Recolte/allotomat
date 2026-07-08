import { inject, onUnmounted, provide, ref, toValue, watchEffect } from "vue";
import type { InjectionKey, MaybeRefOrGetter, Ref } from "vue";

export type KioskIdleTimeoutMode = "disabled" | "idle" | "purchase-complete";

export type KioskShellState = {
  loading: Ref<boolean>;
  showCancel: Ref<boolean>;
  onCancel: Ref<(() => void) | null>;
  idleTimeoutMode: Ref<KioskIdleTimeoutMode>;
};

const kioskShellKey: InjectionKey<KioskShellState> = Symbol("kiosk-shell");

export function provideKioskShell(): KioskShellState {
  const state: KioskShellState = {
    loading: ref(false),
    showCancel: ref(false),
    onCancel: ref(null),
    idleTimeoutMode: ref("idle")
  };

  provide(kioskShellKey, state);
  return state;
}

export function useKioskShellState(
  options: {
    loading?: MaybeRefOrGetter<boolean>;
    showCancel?: MaybeRefOrGetter<boolean>;
    onCancel?: () => void;
    idleTimeoutMode?: MaybeRefOrGetter<KioskIdleTimeoutMode>;
  } = {}
) {
  const shell = inject(kioskShellKey);
  if (!shell) {
    return;
  }

  watchEffect(() => {
    shell.loading.value = toValue(options.loading) ?? false;
    shell.showCancel.value = toValue(options.showCancel) ?? false;
    shell.onCancel.value = options.onCancel ?? null;
    shell.idleTimeoutMode.value = toValue(options.idleTimeoutMode) ?? "idle";
  });

  onUnmounted(() => {
    shell.loading.value = false;
    shell.showCancel.value = false;
    shell.onCancel.value = null;
    shell.idleTimeoutMode.value = "idle";
  });
}

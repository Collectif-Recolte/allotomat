import { inject, onUnmounted, provide, ref, toValue, watchEffect } from "vue";
import type { InjectionKey, MaybeRefOrGetter, Ref } from "vue";

export type KioskShellState = {
  loading: Ref<boolean>;
  showCancel: Ref<boolean>;
  onCancel: Ref<(() => void) | null>;
};

const kioskShellKey: InjectionKey<KioskShellState> = Symbol("kiosk-shell");

export function provideKioskShell(): KioskShellState {
  const state: KioskShellState = {
    loading: ref(false),
    showCancel: ref(false),
    onCancel: ref(null)
  };

  provide(kioskShellKey, state);
  return state;
}

export function useKioskShellState(
  options: {
    loading?: MaybeRefOrGetter<boolean>;
    showCancel?: MaybeRefOrGetter<boolean>;
    onCancel?: () => void;
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
  });

  onUnmounted(() => {
    shell.loading.value = false;
    shell.showCancel.value = false;
    shell.onCancel.value = null;
  });
}

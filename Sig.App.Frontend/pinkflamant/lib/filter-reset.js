import { inject, provide, readonly, ref } from "vue";

export const FilterResetKey = Symbol("FilterReset");

export function provideFilterReset() {
  const resetToken = ref(0);

  provide(FilterResetKey, readonly(resetToken));

  function resetFilters() {
    resetToken.value++;
  }

  return { resetFilters };
}

export function useFilterReset() {
  return inject(FilterResetKey, null);
}

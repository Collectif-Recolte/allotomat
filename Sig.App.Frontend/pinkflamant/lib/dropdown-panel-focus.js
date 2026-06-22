import { inject, onBeforeUnmount, onMounted, shallowRef, provide, nextTick } from "vue";

export const DropdownPanelFocusKey = Symbol("DropdownPanelFocus");

/** À appeler dans le composant conteneur (ex. filter-select) */
export function provideDropdownPanelFocus() {
  const focusHandler = shallowRef(null);

  provide(DropdownPanelFocusKey, {
    register(handler) {
      focusHandler.value = handler;
    },
    unregister() {
      focusHandler.value = null;
    }
  });

  function requestContentFocus() {
    nextTick(() => focusHandler.value?.());
  }

  return { requestContentFocus };
}

/** À appeler dans un enfant optionnel (ex. checkbox-group searchable) */
export function useDropdownPanelFocus(focus) {
  const panelFocus = inject(DropdownPanelFocusKey, null);
  if (!panelFocus) return;

  onMounted(() => panelFocus.register(focus));
  onBeforeUnmount(() => panelFocus.unregister());
}

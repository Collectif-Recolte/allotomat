<template>
  <div>
    <slot></slot>
    <!-- Le conteneur doit posséder les class "min-h-[valeur] relative overflow-hidden" -->
    <Transition v-bind="loadingTransition">
      <div
        v-if="props.loading"
        class="absolute left-0 md:left-64 right-0 top-16 bottom-0 overflow-hidden"
        :class="{ dark: isDark }">
        <div class="absolute z-10 inset-0 h-full w-full bg-white/80 dark:bg-primary-700/80 flex justify-center items-center">
          <span class="pf-animation-loader"></span>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { defineProps } from "vue";

const props = defineProps({
  loading: Boolean,
  isFullHeight: Boolean,
  isDark: Boolean
});

// Style de transitions
const loadingTransition = {
  enterActiveClass: "transition ease-out duration-300",
  enterFromClass: "opacity-0 -translate-y-5 motion-reduce:translate-y-0",
  enterToClass: "opacity-100 translate-y-0",
  leaveActiveClass: "transition duration-700 ease-[cubic-bezier(1,0.5,0.8,1)]",
  leaveFromClass: "opacity-100 translate-y-0",
  leaveToClass: "opacity-0 -translate-y-5 motion-reduce:translate-y-0",
  mode: "out-in"
};
</script>

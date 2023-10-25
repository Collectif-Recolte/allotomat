<template>
  <div class="flex gap-2 items-center">
    <span class="text-primary-700">
      <PfIcon :icon="ICON_INFO" />
    </span>
    <slot>
      <p class="text-p4 leading-tight mb-0">{{ message }}</p>
    </slot>
  </div>
</template>

<script>
import ICON_INFO from "@/lib/icons/info.json";

export default {
  props: {
    message: { type: String, default: "" }
  },
  data() {
    return {
      ICON_INFO
    };
  }
};
</script>

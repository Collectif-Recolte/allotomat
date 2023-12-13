<template>
  <td class="text-p4 py-2" :style="props.rowPaddingBottom">
    <UiGenericContactInfo :person="props.person" />
  </td>
</template>

<script setup>
import { defineProps } from "vue";

const props = defineProps({
  person: {
    type: Object,
    required: true
  },
  rowPaddingBottom: {
    type: Object,
    default: null
  }
});
</script>

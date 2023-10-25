<template>
  <td class="text-p4 py-2" :style="props.rowPaddingBottom">
    <template v-if="email || phone || address || postalCode">
      <!-- eslint-disable-next-line vue/no-v-html -->
      <p v-if="email" class="mb-0" v-html="email"></p>
      <p v-if="phone" class="mb-0">{{ phone }}</p>
      <p v-if="address" class="mb-0">{{ address }}</p>
      <p v-if="postalCode" class="mb-0">{{ postalCode }}</p>
    </template>
    <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
    <p v-else>-</p>
  </td>
</template>

<script setup>
import { defineProps, computed } from "vue";

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

const email = computed(() => (props.person.email ? `<a href="mailto:${props.person.email}">${props.person.email}</a>` : ""));

const phone = computed(() => (props.person.phone ? `${props.person.phone}` : ""));

const address = computed(() => (props.person.address ? `${props.person.address}` : ""));

const postalCode = computed(() => (props.person.postalCode ? `${props.person.postalCode}` : ""));
</script>

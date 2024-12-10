<i18n>
  {
    "en": {
    },
    "fr": {
    }
  }
</i18n>

<template>
  <ul class="mb-6">
    <li v-for="cashRegister in cashRegisters" :key="cashRegister.id" class="mb-4 last:mb-0">
      <CashRegisterItem :cash-register="cashRegister" />
    </li>
  </ul>
</template>

<script setup>
import { defineProps } from "vue";

import CashRegisterItem from "@/components/cash-register/cash-register-item";

defineProps({
  cashRegisters: {
    type: Array,
    required: true,
    default() {
      return [];
    }
  }
});
</script>

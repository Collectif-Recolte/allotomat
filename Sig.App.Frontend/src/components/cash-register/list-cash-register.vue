<i18n>
  {
    "en": {
    },
    "fr": {
    }
  }
</i18n>

<template>
  <div class="flex flex-col relative mb-6">
    <CashRegisterItem v-for="cashRegister in cashRegisters" :key="cashRegister.id" :cash-register="cashRegister" />
  </div>
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

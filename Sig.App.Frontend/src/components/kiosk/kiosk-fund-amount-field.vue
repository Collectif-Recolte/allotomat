<template>
  <KioskFundInputCard :fund="props.fund" :is-gift-card="props.isGiftCard">
    <Field
      :id="inputId"
      v-slot="{ field, errors: fieldErrors, handleChange, handleBlur }"
      validate-on-input
      :name="`funds[${props.idx}].amount`">
      <label
        :for="inputId"
        class="relative bg-white rounded-xl border-2 flex items-center px-4 py-3 min-h-[48px] cursor-text"
        :class="[
          fieldErrors.length ? 'border-red-600 focus-within:border-red-600' : 'border-grey-300 focus-within:border-grey-600'
        ]">
        <input
          :id="inputId"
          :name="field.name"
          :value="field.value ?? ''"
          type="text"
          inputmode="decimal"
          autocomplete="off"
          enterkeyhint="done"
          class="w-full text-right text-h1 font-bold placeholder:text-grey-400 placeholder:font-normal border-0 p-0 pr-8 focus:ring-0 bg-transparent"
          :class="fieldErrors.length ? 'text-red-600' : 'text-primary-900'"
          placeholder="0"
          @blur="(e) => onAmountBlur(e, handleChange, handleBlur)"
          @input="handleChange" />
        <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
        <span
          class="pointer-events-none absolute inset-y-0 right-4 flex items-center text-h3 sm:text-h2 font-bold"
          :class="fieldErrors.length ? 'text-red-600' : 'text-grey-400'"
          aria-hidden="true">
          $
        </span>
      </label>
      <p v-if="fieldErrors.length" class="text-red-600 text-p4 mt-1 mb-0 text-right">{{ fieldErrors[0] }}</p>
    </Field>
  </KioskFundInputCard>
</template>

<script setup>
import { computed, defineProps } from "vue";

import KioskFundInputCard from "@/components/kiosk/kiosk-fund-input-card";

const props = defineProps({
  idx: { type: Number, required: true },
  fund: { type: Object, required: true },
  isGiftCard: Boolean
});

const inputId = computed(() => `funds[${props.idx}].amount`);

/** Keep at most two digits after the decimal separator (. or ,). */
function limitToTwoDecimals(value) {
  return String(value ?? "").replace(/([.,]\d{2})\d+/g, "$1");
}

function onAmountBlur(event, handleChange, handleBlur) {
  let value = limitToTwoDecimals(event.target.value);
  const normalized = String(value).replace(/,/, ".").trim();
  if (normalized !== "" && !Number.isNaN(Number(normalized)) && Number(normalized) === 0) {
    value = "";
  }
  if (value !== event.target.value) {
    event.target.value = value;
    handleChange(event);
  }
  handleBlur(event);
}
</script>

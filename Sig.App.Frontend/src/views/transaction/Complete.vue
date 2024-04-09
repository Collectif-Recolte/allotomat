<i18n>
{
	"en": {
	},
	"fr": {
	}
}
</i18n>

<template>
  <div class="min-h-app px-4 xs:px-8 py-10 flex items-center justify-center">
    <div class="bg-white rounded-2xl pt-6 pb-3 px-3 h-remove-margin relative xs:p-6 w-full xs:w-auto xs:min-w-[460px]">
      <UiIconComplete />
      <CompleteTransaction
        :transaction-id="props.transactionId"
        @onUpdateStep="(stepName, values) => emit('onUpdateStep', stepName, values)"
        @onUpdateLoadingState="(e) => emit('onUpdateLoadingState', e)" />
    </div>
  </div>
</template>

<script setup>
import { defineEmits, defineProps } from "vue";

import CompleteTransaction from "@/components/transaction/complete-transaction";

const emit = defineEmits(["onUpdateStep", "onUpdateLoadingState"]);

const props = defineProps({
  transactionId: {
    type: String,
    required: true
  }
});
</script>

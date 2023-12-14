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
        :transactionId="props.transactionId"
        @updateStep="(e) => emit('onUpdateStep', e)"
        @updateLoadingState="(e) => emit('onUpdateLoadingState', e)" />
    </div>
  </div>
</template>

<script setup></script>

É
<i18n>
  {
    "en": {
      "project-name": "Program",
      "market-group-name": "Market group",
      "is-inactif-tag": "Disabled",
      "is-active-tag": "Enabled",
      "is-disabled-tooltip": "This cash register is no longer part of any market group."
    },
    "fr": {
      "project-name": "Programme",
      "market-group-name": "Groupe de commerce",
      "is-inactif-tag": "Désactivé",
      "is-active-tag": "Activé",
      "is-disabled-tooltip": "Cette caisse ne fait plus partie d'aucun groupe de commerce."
    }
  }
</i18n>

<template>
  <div v-if="props.cashRegister" class="relative border border-primary-300 rounded-lg px-5 pt-3 pb-6 mb-4 last:mb-0">
    <div class="space-y-6 divide-y divide-grey-300">
      <h3 class="text-h4 text-primary-900 mt-2 mb-2">
        <span>{{ props.cashRegister.name }}</span>
      </h3>
      <div v-for="marketGroup in props.cashRegister.marketGroups" :key="marketGroup.id" class="space-y-4">
        <div class="mt-4">
          <dt :class="dtClasses">{{ t("project-name") }}</dt>
          <dd :class="ddClasses">{{ marketGroup.project.name }}</dd>
        </div>
        <div>
          <dt :class="dtClasses">{{ t("market-group-name") }}</dt>
          <dd :class="ddClasses">{{ marketGroup.name }}</dd>
        </div>
      </div>
    </div>
    <div class="absolute right-3 top-3">
      <PfTooltip
        class="group-pfone"
        :hide-tooltip="props.cashRegister.marketGroups.length !== 0"
        :label="t('is-disabled-tooltip')">
        <PfTag
          :label="props.cashRegister.marketGroups.length === 0 ? t('is-inactif-tag') : t('is-active-tag')"
          :bg-color-class="props.cashRegister.marketGroups.length === 0 ? 'bg-red-300' : 'bg-primary-300'" />
      </PfTooltip>
      <CashRegisterActions v-if="!cashRegister.isArchived" class="ml-2" :cash-register="cashRegister" />
    </div>
  </div>
</template>

<script setup>
import { defineProps } from "vue";
import { useI18n } from "vue-i18n";

import CashRegisterActions from "@/components/cash-register/cash-register-actions";

const dtClasses = "text-primary-500 font-semibold tracking-tight mt-px sm:mt-[3px]";
const ddClasses = "text-primary-900";

const { t } = useI18n();

const props = defineProps({
  cashRegister: {
    type: Object,
    required: true
  }
});
</script>

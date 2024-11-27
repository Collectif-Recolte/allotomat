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
  <div v-if="cashRegister" class="border border-primary-300 rounded-lg p-4 space-y-4 divide-y divide-grey-300">
    <div class="flex justify-between gap-x-4 items-center">
      <h3 class="text-h4 font-semibold text-primary-900 mt-2 mb-2">
        <span>{{ cashRegister.name }}</span>
      </h3>
      <div class="flex">
        <PfTooltip
          class="group-pfone"
          :hide-tooltip="cashRegister.marketGroups.length !== 0"
          :label="t('is-disabled-tooltip')"
          position="left">
          <PfTag
            :label="cashRegister.marketGroups.length === 0 ? t('is-inactif-tag') : t('is-active-tag')"
            :bg-color-class="cashRegister.marketGroups.length === 0 ? 'bg-red-300' : 'bg-primary-300'" />
        </PfTooltip>
        <CashRegisterActions v-if="!cashRegister.isArchived" class="ml-2" :cash-register="cashRegister" />
      </div>
    </div>
    <dl v-if="cashRegister.marketGroups.length > 0">
      <div v-for="marketGroup in cashRegister.marketGroups" :key="marketGroup.id" class="space-y-3">
        <div class="mt-4">
          <dt :class="dtClasses">{{ t("project-name") }}</dt>
          <dd :class="ddClasses">{{ marketGroup.project.name }}</dd>
        </div>
        <div>
          <dt :class="dtClasses">{{ t("market-group-name") }}</dt>
          <dd :class="ddClasses">{{ marketGroup.name }}</dd>
        </div>
      </div>
    </dl>
  </div>
</template>

<script setup>
import { defineProps } from "vue";
import { useI18n } from "vue-i18n";

import CashRegisterActions from "@/components/cash-register/cash-register-actions";

const dtClasses = "text-primary-700 text-p2";
const ddClasses = "text-primary-900 text-h4 leading-normal";

const { t } = useI18n();

defineProps({
  cashRegister: {
    type: Object,
    required: true
  }
});
</script>

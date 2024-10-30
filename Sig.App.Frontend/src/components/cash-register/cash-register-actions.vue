<i18n>
  {
    "en": {
      "cash-register-edit": "Edit",
      "cash-register-archive": "Archive",
      "cash-register-cant-archive": "This cash register can't be archived since it's the last one available."
    },
    "fr": {
      "cash-register-edit": "Modifier",
      "cash-register-archive": "Archiver",
      "cash-register-cant-archive": "Cette caisse ne peut pas être archivée puisqu'elle est la dernière disponible."
    }
  }
</i18n>

<template>
  <UiButtonGroup :items="items" />
</template>

<script setup>
import { defineProps, ref, onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";

import ICON_PENCIL from "@/lib/icons/pencil.json";
import ICON_TRASH from "@/lib/icons/trash.json";

import { URL_CASH_REGISTER_EDIT, URL_CASH_REGISTER_ARCHIVE } from "@/lib/consts/urls";

const { t } = useI18n();

const items = ref([]);

onMounted(() => {
  updateItems();
});

watch(
  () => props.cashRegister,
  () => {
    updateItems();
  }
);

function updateItems() {
  items.value = [
    {
      isExtra: true,
      icon: ICON_PENCIL,
      label: t("cash-register-edit"),
      route: { name: URL_CASH_REGISTER_EDIT, params: { cashRegisterId: props.cashRegister.id } }
    },
    {
      isExtra: true,
      icon: ICON_TRASH,
      label: t("cash-register-archive"),
      route: { name: URL_CASH_REGISTER_ARCHIVE, params: { cashRegisterId: props.cashRegister.id } },
      disabled: !props.cashRegister.cashRegisterCanBeDelete,
      reason: t("cash-register-cant-archive")
    }
  ];
}

const props = defineProps({
  cashRegister: {
    type: Object,
    required: true
  }
});
</script>

<style scoped>
.disable_link {
  pointer-events: none;
  opacity: 0.5;
}
</style>

<i18n>
  {
    "en": {
      "add": "Add",
      "empty-list-error": "The list must contain at least 1 element",
      "delete": "Delete",
    },
    "fr": {
      "add": "Ajouter",
      "empty-list-error": "La liste doit contenir au moins 1 élément",
      "delete": "Supprimer",
    }
  }
  </i18n>

<template>
  <div class="flex flex-col gap-y-6 relative">
    <p v-if="fields.length === 0" class="mb-0 text-sm text-red-500">{{ emptyListError || t("empty-list-error") }}</p>
    <div
      v-for="(field, idx) in fields"
      v-else
      :key="idx"
      class="flex flex-col relative"
      :class="[
        isInsideBlockLayout ? '' : 'bg-secondary-100 p-4 pt-3 rounded-lg',
        blockLayout ? 'gap-y-6' : 'xs:flex-row xs:items-start gap-y-3 gap-x-4 xs:bg-transparent xs:p-0 xs:rounded-none'
      ]">
      <slot :idx="idx"></slot>

      <div v-if="!cantDelete && blockLayout" class="absolute -top-3 -right-3 lg:-top-4 lg:-right-4">
        <PfTooltip class="group-pfone" :label="deleteLabel || t('delete')" position="left">
          <PfButtonAction
            is-icon-only
            size="sm"
            :icon="ICON_MINUS"
            class="rounded-full min-h-8 min-w-8 p-0"
            @click="() => emit('removeField', idx)" />
        </PfTooltip>
      </div>
      <UiButtonGroup
        v-if="!cantDelete && !blockLayout"
        class="justify-end ml-auto xs:mt-6"
        btn-size="md"
        :items="getBtnGroup(idx)"
        tooltip-position="left" />
    </div>
    <PfButtonAction
      v-if="!cantAdd"
      btn-style="dash"
      has-icon-left
      :icon="ICON_PLUS"
      type="button"
      :label="addLabel || t('add')"
      @click="() => emit('addField')" />
    <p v-if="hasErrorState" class="mb-0 text-sm text-red-500 border-red-300">
      {{ errors }}
    </p>
  </div>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { defineEmits, defineProps, computed } from "vue";

import ICON_PLUS from "@/lib/icons/plus.json";
import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_MINUS from "@/lib/icons/minus-plain.json";

const { t } = useI18n();

const emit = defineEmits(["addField", "removeField"]);
const props = defineProps({
  addLabel: { type: String, default: "" },
  deleteLabel: { type: String, default: "" },
  emptyListError: { type: String, default: "" },
  fields: { type: Array, default: null },
  cantDelete: { type: Boolean, default: false },
  cantAdd: { type: Boolean, default: false },
  blockLayout: Boolean,
  isInsideBlockLayout: Boolean,
  errors: { type: String, default: "" }
});

var hasErrorState = computed(() => {
  return props.errors && props.errors.length > 0;
});

const getBtnGroup = (idx) => [
  {
    icon: ICON_TRASH,
    label: props.deleteLabel || t("delete"),
    onClick: () => emit("removeField", idx)
  }
];
</script>

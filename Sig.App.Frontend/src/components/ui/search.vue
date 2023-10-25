<i18n>
  {
    "en": {
      "label": "Search",
      "placeholder": "Search by email, ID, name...",
      "placeholder-anonymous": "Search by ID...",
      "btn-label": "Search"
    },
    "fr": {
      "label": "Recherche",
      "placeholder": "Chercher par courriel, ID, nom...",
      "placeholder-anonymous": "Chercher par ID...",
      "btn-label": "Rechercher"
    }
  }
</i18n>

<template>
  <PfFormInputText
    :id="props.id"
    class="min-w-60 xs:min-w-72"
    :label="t('label')"
    :value="modelValue"
    has-hidden-label
    :placeholder="!beneficiariesAreAnonymous ? t('placeholder') : t('placeholder-anonymous')"
    @input="(e) => emit('update:modelValue', e)"
    @keyup.enter="() => emit('search')">
    <template #trailingIcon>
      <div class="absolute inset-y-0 right-3 flex items-center text-primary-700">
        <PfButtonAction
          class="relative z-10 min-w-0"
          is-icon-only
          size="sm"
          btn-style="link"
          :aria-labelledby="t('btn-label')"
          :icon="ICON_SEARCH"
          @click="() => emit('search')" />
      </div>
    </template>
  </PfFormInputText>
</template>

<script setup>
import { defineEmits, defineProps } from "vue";
import { useI18n } from "vue-i18n";

import ICON_SEARCH from "@/lib/icons/search.json";

const { t } = useI18n();

const emit = defineEmits(["search", "update:modelValue"]);

const props = defineProps({
  id: {
    type: String,
    default: "searchInput"
  },
  modelValue: {
    type: String,
    default: ""
  },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  }
});
</script>

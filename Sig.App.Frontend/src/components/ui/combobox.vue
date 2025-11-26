<i18n>
  {
    "en": {
      "nothing-found": "Nothing found."
    },
    "fr": {
      "nothing-found": "Aucun résultat trouvé."
    }
  }
  </i18n>

<template>
  <FormField
    :id="id"
    v-slot="{ hasErrorState }"
    :label="label"
    :description="description"
    :col-span-class="colSpanClass"
    :errors="errors"
    :disabled="disabled"
    :has-hidden-label="hasHiddenLabel"
    :required="required">
    <Combobox v-model="selected" @update:modelValue="updateSelected">
      <div>
        <div>
          <ComboboxInput
            class="pf-select text-[18px] min-h-11 shadow-sm block w-full rounded-md transition-colors duration-200 ease-in-out disabled:bg-grey-100 disabled:text-grey-700"
            :class="
              hasErrorState
                ? 'border-3 text-red-600 border-red-600 placeholder-red-300 focus:ring-red-600 focus:border-red-600'
                : 'text-primary-900 border-primary-500 focus:ring-secondary-500 focus:border-secondary-500 placeholder-grey-500'
            "
            :display-value="(item) => item.label"
            @change="query = $event.target.value"
            @click="isOpen = true"
            @focus="isOpen = true"
            @blur="isOpen = false">
            <ComboboxButton class="inset-y-0 right-0 flex items-center pr-2">
              <PfIcon class="h-5 w-5 text-gray-400" aria-hidden="true" :icon="CHEVRON_DOWN_ICON" />
            </ComboboxButton>
          </ComboboxInput>
        </div>
        <ComboboxOptions
          v-show="isOpen"
          static
          class="absolute mt-1 max-h-60 w-full overflow-auto rounded-md bg-white py-1 text-base shadow-lg ring-1 ring-black/5 focus:outline-none sm:text-sm">
          <div
            v-if="filteredOptions.length === 0 && query !== ''"
            class="relative cursor-default select-none px-4 py-2 text-gray-700">
            {{ t("nothing-found") }}
          </div>
          <ComboboxOption
            v-for="option in filteredOptions"
            :key="option.id"
            v-slot="{ selected, active }"
            as="template"
            :value="option.value">
            <li
              class="relative cursor-default select-none py-2 px-4"
              :class="{
                'bg-primary-50 text-black dark:bg-primary-700 dark:text-white': active,
                'text-gray-900': !active
              }">
              <span class="block truncate" :class="{ 'font-medium': selected, 'font-normal': !selected }">
                {{ option.label }}
              </span>
              <span
                v-if="selected"
                class="absolute inset-y-0 left-0 flex items-center"
                :class="{ 'text-white': active, 'text-primary-500': !active }">
                <PfIcon class="h-5 w-5" aria-hidden="true" :icon="CHECK_ICON" />
              </span>
            </li>
          </ComboboxOption>
        </ComboboxOptions>
      </div>
    </Combobox>
  </FormField>
</template>

<script setup>
import { ref, computed, defineProps, defineEmits } from "vue";
import { Combobox, ComboboxInput, ComboboxButton, ComboboxOptions, ComboboxOption } from "@headlessui/vue";
import { useI18n } from "vue-i18n";
import FormField from "@/../pinkflamant/components/form/field/index";
import { commonFieldProps } from "@/../pinkflamant/components/form/field/index";

import CHECK_ICON from "@/lib/icons/chevron-right.json";
import CHEVRON_DOWN_ICON from "@/lib/icons/chevron-down.json";

const { t } = useI18n();

const emit = defineEmits(["input"]);

const props = defineProps({
  ...commonFieldProps,
  options: {
    type: Array,
    required: true
  },
  value: {
    type: String,
    default: ""
  }
});

let selected = ref(props.options.find((option) => option.value === props.value));
let query = ref("");
let isOpen = ref(false);

function updateSelected(value) {
  selected.value = props.options.find((option) => option.value === value);
  emit("input", value);
  isOpen.value = false;
}

let filteredOptions = computed(() =>
  query.value === ""
    ? props.options
    : props.options.filter((option) =>
        option.label.toLowerCase().replace(/\s+/g, "").includes(query.value.toLowerCase().replace(/\s+/g, ""))
      )
);
</script>

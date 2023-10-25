<template>
  <div>
    <Listbox :model-value="value || modelValue" @update:modelValue="updateSelectedColor">
      <div class="relative">
        <ListboxLabel class="block font-semibold text-sm mb-1 transition-colors duration-200 ease-in-out text-primary-900">{{
          props.label
        }}</ListboxLabel>
        <ListboxButton
          class="cursor-default flex gap-x-2.5 items-center bg-transparent leading-none rounded-md relative transition duration-200 ease-in-out ring-offset-2 outline-0 border text-[18px] px-3 min-h-11 shadow-sm w-full disabled:bg-grey-100 disabled:text-grey-700 focus-visible:ring-2 text-primary-900 border-primary-500 focus:ring-secondary-500 focus:border-secondary-500">
          <UiColorChip
            v-if="value || modelValue"
            :color="value ? getColorBgClass(value) : getColorBgClass(modelValue)"
            :has-aria-label="false" />
          <span v-if="value || modelValue" class="block">{{ value ? getColorName(value) : getColorName(modelValue) }}</span>
          <span class="pointer-events-none flex items-center ml-auto">
            <PfIcon :icon="ICON_ARROW_BOTTOM" size="xxs" aria-hidden="true" />
          </span>
        </ListboxButton>

        <transition
          leave-active-class="transition duration-100 ease-in"
          leave-from-class="opacity-100"
          leave-to-class="opacity-0">
          <ListboxOptions
            v-if="options && options.length > 0"
            class="absolute my-1 max-h-40 w-full overflow-auto rounded-md border broder-primary-700 bg-white py-1 text-base shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none">
            <ListboxOption v-for="(option, index) in options" v-slot="{ active }" :key="index" :value="option" as="template">
              <li
                :class="[
                  active ? 'bg-secondary-100 text-primary-900' : 'text-primary-700',
                  'relative cursor-default select-none py-1 px-3 flex items-center gap-x-2.5'
                ]">
                <UiColorChip :color="option.colorBgClass" :has-aria-label="false" />
                <span :class="[option.value === value ? 'font-medium' : 'font-normal', 'block truncate']">{{
                  option.label
                }}</span>
              </li>
            </ListboxOption>
          </ListboxOptions>
        </transition>
      </div>
    </Listbox>
  </div>
</template>

<script setup>
import { defineProps, defineEmits } from "vue";
import { Listbox, ListboxLabel, ListboxButton, ListboxOptions, ListboxOption } from "@headlessui/vue";
import ICON_ARROW_BOTTOM from "@/lib/icons/arrow-bottom.json";
import { getColorName, getColorBgClass } from "@/lib/helpers/products-color";

const props = defineProps({
  label: {
    type: String,
    required: true
  },
  options: {
    type: Array,
    required: true
  },
  modelValue: {
    type: null,
    default: null
  },
  value: {
    type: null,
    default: null
  }
});

const emit = defineEmits(["update:modelValue"]);

function updateSelectedColor(e) {
  emit("update:modelValue", e.value);
}
</script>

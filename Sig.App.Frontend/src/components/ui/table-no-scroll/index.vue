<template>
  <div class="pf-table-no-scroll flex flex-col relative -mx-section md:-mx-8">
    <div class="pb-2 align-middle inline-block w-full px-section md:px-8">
      <div class="" :class="{ 'pb-12': slots.floatingActions || hasBottomPadding }">
        <table class="w-full max-w-full divide-y-2 divide-primary-900 dark:divide-grey-700">
          <slot name="caption">
            <caption v-if="props.caption">
              {{
                props.caption
              }}
            </caption>
          </slot>
          <thead class="sticky z-10 top-16 bg-grey-50">
            <tr
              class="relative after:absolute after:left-full after:-right-section md:after:-right-8 after:inset-y-0 after:bg-grey-50">
              <th
                v-for="(col, index) in props.cols"
                :key="index"
                scope="col"
                class="px-6 py-4 group-one first:pl-0 last:pr-0 text-p2 font-semibold text-primary-900 dark:text-grey-300 relative"
                :class="[col.hasHiddenLabel ? 'opacity-0' : '', col.isRight ? 'text-right' : 'text-left']">
                <div class="hidden group-one-first:block bg-grey-50 absolute inset-y-0 -left-section right-full md:-left-8"></div>
                {{ col.label }}
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-grey-200 dark:bg-grey-900 dark:divide-grey-700">
            <tr v-for="(item, index) in props.items" :key="index">
              <slot :item="item" />
            </tr>
          </tbody>
          <tfoot v-if="props.footers.length > 0" class="sticky z-10 bottom-0 bg-grey-50">
            <tr
              class="relative after:absolute after:left-full after:-right-section md:after:-right-8 after:inset-y-0 after:bg-grey-50">
              <th
                v-for="(footer, index) in props.footers"
                :key="index"
                scope="col"
                class="px-6 py-4 group-one first:pl-0 last:pr-0 text-p2 font-semibold text-primary-900 dark:text-grey-300 relative"
                :class="[footer.hasHiddenLabel ? 'sr-only' : '', footer.isRight ? 'text-right' : 'text-left']">
                <div class="hidden group-one-first:block bg-grey-50 absolute inset-y-0 -left-section right-full md:-left-8"></div>
                {{ footer.value }}
              </th>
            </tr>
          </tfoot>
        </table>
      </div>
    </div>

    <div
      v-if="slots.floatingActions"
      class="sticky bottom-0 right-0 before:block before:absolute before:w-[calc(100%+50px)] before:h-[calc(100%+50px)] before:-translate-y-1/2 before:right-0 before:top-1/2 before:bg-gradient-radial before:bg-white/70 before:blur-lg before:rounded-full">
      <slot name="floatingActions"></slot>
    </div>
  </div>
</template>

<script setup>
import { defineProps, useSlots } from "vue";

const props = defineProps({
  caption: {
    type: String,
    default: ""
  },
  cols: {
    type: Array,
    default() {
      return [];
    }
  },
  items: {
    type: Array,
    default() {
      return [];
    }
  },
  footers: {
    type: Array,
    default() {
      return [];
    }
  },
  hasBottomPadding: Boolean
});

const slots = useSlots();
</script>

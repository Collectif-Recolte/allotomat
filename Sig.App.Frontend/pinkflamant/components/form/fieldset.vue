<template>
  <fieldset class="h-remove-margin">
    <legend
      v-if="legend || $slots.legend"
      class="text-xs uppercase font-bold tracking-wide transition-colors duration-200 ease-in-out"
      :class="hasErrorState ? 'text-red-500' : 'text-primary-900'"
      :aria-describedby="description ? `${id}-legend-description` : null">
      <slot name="legend">
        <span>{{ legend }}</span>
        <span v-if="errors.length > 0" class="sr-only"> {{ errors[0] }}</span>
      </slot>
    </legend>
    <p
      v-if="description || $slots.description"
      :id="`${id}-legend-description`"
      class="text-sm transition-colors duration-200 ease-in-out"
      :class="hasErrorState ? 'text-red-500' : 'text-grey-500'">
      <slot name="description">{{ description }}</slot>
    </p>

    <div :class="isFilter ? 'mt-2 space-y-2' : 'mt-4 space-y-4'">
      <slot></slot>
    </div>

    <!-- Fieldset footer -->
    <transition name="fade">
      <div v-if="errors.length > 0 || $slots.feedback" class="mt-2 text-red-500">
        <slot name="feedback">
          <p v-if="errors.length > 0" class="mb-0 text-sm" aria-hidden="true">
            {{ errors[0] }}
          </p>
        </slot>
      </div>
    </transition>
  </fieldset>
</template>

<script>
export default {
  props: {
    id: {
      type: String,
      default: ""
    },
    legend: {
      type: String,
      default: ""
    },
    description: {
      type: String,
      default: ""
    },
    errors: {
      type: Array,
      default() {
        return [];
      }
    },
    hasErrorState: Boolean,
    isFilter: Boolean
  }
};
</script>

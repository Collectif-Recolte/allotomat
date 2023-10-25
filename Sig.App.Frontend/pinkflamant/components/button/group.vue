<template>
  <div class="shadow-sm inline-flex rounded-md">
    <template v-if="items && items.length > 0">
      <slot name="buttons">
        <Tooltip
          v-for="(item, index) in items"
          :key="index"
          v-slot="{ tooltipId }"
          :label="item.label"
          :position="tooltipPosition"
          class="group-pfone -ml-px first:ml-0">
          <ButtonAction
            v-if="item.onClick"
            class="group-pfone-first:rounded-l-md group-pfone-last:rounded-r-md"
            :class="btnCustomClasses"
            is-icon-only
            is-grouped
            btn-style="outline"
            :size="btnSize"
            :icon="item.icon"
            :aria-labelledby="tooltipId"
            :is-disabled="item.disabled"
            v-bind="item.attrs"
            @click="item.onClick" />
          <ButtonLink
            v-else
            tag="RouterLink"
            class="group-pfone-first:rounded-l-md group-pfone-last:rounded-r-md"
            :to="item.route"
            btn-style="outline"
            :size="btnSize"
            is-icon-only
            is-grouped
            :icon="item.icon"
            :aria-labelledby="tooltipId"
            :is-disabled="item.disabled"
            v-bind="item.attrs" />
        </Tooltip>
      </slot>
    </template>

    <slot></slot>
  </div>
</template>

<script>
import ButtonAction from "./action";
import ButtonLink from "./link";
import Tooltip from "../tooltip";

export default {
  components: {
    ButtonAction,
    ButtonLink,
    Tooltip
  },
  props: {
    items: {
      type: Array,
      default() {
        [];
      }
    },
    tooltipPosition: {
      type: String,
      default: "top"
    },
    btnSize: {
      type: String,
      default: "sm"
    },
    btnCustomClasses: {
      type: Array,
      default() {
        [];
      }
    }
  }
};
</script>

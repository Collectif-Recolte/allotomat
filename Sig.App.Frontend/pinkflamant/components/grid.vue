<template>
  <div
    class="pf-grid flex items-stretch flex-grow flex-wrap -mx-grid-gap -mb-10"
    :class="{ 'pf-grid--small-gap': hasSmallGap, 'justify-center': isCentered }">
    <div v-for="(item, index) in items" :key="index" class="pf-grid__col mx-grid-gap mb-10" :class="pfGridClass">
      <slot :item="item"></slot>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    hasSmallGap: Boolean,
    isCentered: Boolean,
    items: {
      type: Array,
      default() {
        return [];
      },
      required: true
    },
    itemsPerRow: {
      type: Number,
      default: 2,
      validator(value) {
        return value <= 7;
      }
    }
  },
  computed: {
    pfGridClass() {
      switch (this.itemsPerRow) {
        case 2:
          return "pf-grid__col--col-2";
        case 3:
          return "pf-grid__col--col-3";
        case 4:
          return "pf-grid__col--col-4";
        case 5:
          return "pf-grid__col--col-5";
        case 6:
          return "pf-grid__col--col-6";
        case 7:
          return "pf-grid__col--col-7";
        default:
          return "";
      }
    }
  }
};
</script>

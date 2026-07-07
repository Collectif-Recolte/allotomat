<script>
import { defineComponent, watch } from "vue";

export default defineComponent({
  name: "SlotPropWatcher",
  props: {
    value: {
      required: true
    },
    filter: {
      type: Function,
      required: true
    }
  },
  emits: ["change"],
  setup(props, { emit }) {
    watch(
      () => props.value,
      (value, previousValue) => {
        if (props.filter(value, previousValue)) {
          emit("change");
        }
      }
    );
    return () => null;
  }
});
</script>

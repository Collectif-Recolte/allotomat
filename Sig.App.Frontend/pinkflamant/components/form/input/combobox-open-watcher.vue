<script>
import { defineComponent, watch } from "vue";

export default defineComponent({
  name: "ComboboxOpenWatcher",
  props: {
    open: {
      type: Boolean,
      required: true
    }
  },
  emits: ["close"],
  setup(props, { emit }) {
    watch(
      () => props.open,
      (open, wasOpen) => {
        if (wasOpen && !open) {
          emit("close");
        }
      }
    );
    return () => null;
  }
});
</script>

// Watch body class "dark" to adjust component color theme
import { ref, readonly, onMounted, onUnmounted } from "vue";

export function useDarkMode() {
  const dark = ref(false);
  const options = {
    attributes: true
  };
  const observer = new MutationObserver(callback);

  function callback(mutationList) {
    mutationList.forEach(function (mutation) {
      if (mutation.type === "attributes" && mutation.attributeName === "class") {
        dark.value = document.body.classList.contains("dark");
      }
    });
  }

  onMounted(() => {
    observer.observe(document.body, options);
    dark.value = document.body.classList.contains("dark");
  });

  onUnmounted(() => observer.disconnect());

  return readonly(dark);
}

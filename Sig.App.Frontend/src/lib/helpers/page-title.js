// Update page title
import { onMounted, onUnmounted, watchEffect } from "vue";

function setTitle(title) {
  document.title = title;
}

function getCurrentTitle() {
  return document.title;
}

export function usePageTitle(title) {
  const originalTitle = getCurrentTitle();

  onMounted(() => {
    if (typeof title === "function") {
      watchEffect(() => setTitle(title()));
    } else {
      setTitle(title);
    }
  });

  onUnmounted(() => setTitle(originalTitle));
}

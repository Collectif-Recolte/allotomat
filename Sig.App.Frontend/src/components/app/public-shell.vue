<i18n>
  {
    "en": {
      "logo": "Logo"
    },
    "fr": {
      "logo": "Logo"
    }
  }
</i18n>

<template>
  <div class="bg-primary-50">
    <SkipLink />
    <div>
      <div class="h-16 flex items-center px-section md:px-8 border-b border-primary-300 dark:border-primary-900">
        <div class="shrink-0 flex items-center h-16">
          <img class="h-8" :src="require('@/assets/logo/logo-color.svg')" :alt="t('logo')" />
        </div>
      </div>
    </div>

    <div class="min-h-app relative flex items-center justify-center">
      <div
        class="bg-white px-6 py-12 xs:px-8 sm:px-12 sm:py-16 md:px-16 lg:p-20 rounded-3xl max-w-[calc(100%-1.25rem*2)] xs:max-w-[calc(100%-3rem*2)] mb-16 mt-4 xs:my-16">
        <div class="mx-auto max-w-full w-[525px]">
          <!-- Page title -->
          <header v-if="showTitle">
            <slot name="title">
              <h1 class="text-3xl mt-0">{{ props.title }}</h1>
            </slot>
          </header>

          <!-- Main content -->
          <main id="main-content">
            <slot></slot>
          </main>
        </div>
      </div>

      <LangSwitch class="absolute bottom-4 left-0 ml-section md:ml-8" />
    </div>

    <Footer />
  </div>
</template>

<script setup>
import { defineProps, useSlots, computed } from "vue";
import { useI18n } from "vue-i18n";

import LangSwitch from "@/components/app/lang-switch";
import SkipLink from "@/components/app/skip-link.vue";
import Footer from "@/components/app/footer";

const props = defineProps({
  title: {
    type: String,
    default: ""
  }
});

const slots = useSlots();
const { t } = useI18n();

const showTitle = computed(() => !!(slots.title || props.title));
</script>

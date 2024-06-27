<i18n>
{
	"en": {
		"close-sidebar": "Close sidebar",
		"logo": "Tomat logo",
    "support": "Help and support",
	},
	"fr": {
		"close-sidebar": "Fermer le panneau",
		"logo": "Logo de Tomat",
    "support": "Aide et support",
	}
}
</i18n>

<template>
  <div>
    <div class="fixed inset-x-0 z-30">
      <SkipLink />
    </div>

    <!-- Off-canvas menu for mobile, show/hide based on off-canvas menu state. -->
    <TransitionRoot as="template" :show="sidebarOpen">
      <Dialog as="div" class="fixed inset-0 flex z-40 md:hidden" @close="sidebarOpen = false">
        <TransitionChild as="template" v-bind="overlayTransition">
          <DialogOverlay class="transition-opacity fixed inset-0 bg-primary-700/80" />
        </TransitionChild>
        <TransitionChild as="template" v-bind="slidingPanelTransition">
          <div class="relative flex-1 flex flex-col max-w-xs w-full bg-primary-100 dark:bg-grey-800">
            <TransitionChild as="template" v-bind="closeBtnTransition">
              <div class="absolute top-0 right-0 -mr-12 pt-3">
                <button
                  type="button"
                  class="text-grey-200 ml-1 flex items-center justify-center h-10 w-10 rounded-full focus:outline-none focus:ring-2 focus:ring-inset focus:ring-white"
                  @click="sidebarOpen = false">
                  <span class="sr-only">{{ t("close-sidebar") }}</span>
                  <PfIcon size="lg" :icon="CloseIcon" aria-hidden="true" />
                </button>
              </div>
            </TransitionChild>
            <div class="flex-1 h-0 pb-4 overflow-y-auto">
              <div class="shrink-0 flex items-center px-4 h-16">
                <img class="h-8" :src="require('@/assets/logo/logo-color.svg')" :alt="t('logo')" />
              </div>
              <MainMenu />
            </div>

            <SecondaryMenu />
            <div class="shrink-0 flex flex-col gap-y-4 items-start border-t border-primary-300 dark:border-grey-900 p-4">
              <LangSwitch />
              <LogoutBtn />
              <PfButtonLink
                class="no-underline"
                btn-style="link"
                has-icon-left
                :icon="ICON_SUPPORT"
                href="https://allotomat.notion.site/Support-Tomat-c1f01ce94bd549aa92e0a64b624c5507"
                :label="t('support')"
                target="_blank" />
            </div>
          </div>
        </TransitionChild>
        <div class="shrink-0 w-14">
          <!-- Force sidebar to shrink to fit close icon -->
        </div>
      </Dialog>
    </TransitionRoot>

    <!-- Static sidebar for desktop -->
    <div class="hidden md:flex md:w-64 md:flex-col md:fixed md:inset-y-0">
      <div class="flex-1 flex flex-col min-h-0 bg-primary-100">
        <div class="flex-1 flex flex-col pb-8 overflow-y-auto">
          <div class="flex items-center shrink-0 px-4 h-16">
            <img class="h-8" :src="require('@/assets/logo/logo-color.svg')" :alt="t('logo')" />
          </div>
          <MainMenu />
        </div>

        <SecondaryMenu />
        <div class="shrink-0 flex flex-col gap-y-4 items-start border-t border-primary-300 dark:border-grey-900 p-4 pb-8">
          <LangSwitch />
          <LogoutBtn />
          <PfButtonLink
            class="no-underline"
            btn-style="link"
            has-icon-left
            :icon="ICON_SUPPORT"
            href="https://allotomat.notion.site/Support-Tomat-c1f01ce94bd549aa92e0a64b624c5507"
            :label="t('support')"
            target="_blank" />
        </div>
      </div>
    </div>

    <div class="md:pl-64 flex flex-col flex-1">
      <TopBar :hide-profile-menu="hideProfileMenu" @click="sidebarOpen = true" />
      <div class="flex-1">
        <Breadcrumb
          v-if="showBreadcrumbs"
          :breadcrumbs="props.breadcrumbs"
          class="px-section md:px-8 border-b border-grey-200 dark:border-grey-800" />
        <Loading :loading="loading" is-full-height :is-dark="isDark">
          <div class="min-h-app">
            <!-- Page title -->
            <header v-if="showTitle">
              <slot name="title">
                <Title :title="props.title" />
              </slot>
            </header>

            <!-- Main content -->
            <main id="main-content" :class="{ 'px-section md:px-8 py-5': !noPadding }">
              <slot></slot>
            </main>
          </div>
        </Loading>
      </div>
      <Footer :class="{ dark: isDark }" />
    </div>
  </div>
</template>

<script setup>
import { defineProps, ref, useSlots, computed } from "vue";
import { useI18n } from "vue-i18n";

import { Dialog, DialogOverlay, TransitionChild, TransitionRoot } from "@headlessui/vue";
import Breadcrumb from "@/components/app/breadcrumb";
import Loading from "@/components/app/loading";
import LogoutBtn from "@/components/app/logout-btn";
import LangSwitch from "@/components/app/lang-switch";
import MainMenu from "@/components/app/main-menu";
import SecondaryMenu from "@/components/app/secondary-menu";
import SkipLink from "@/components/app/skip-link";
import Title from "@/components/app/title";
import TopBar from "@/components/app/top-bar";
import Footer from "@/components/app/footer";

import ICON_SUPPORT from "@/lib/icons/support.json";
import CloseIcon from "@/lib/icons/close.json";

const props = defineProps({
  breadcrumbs: { type: Array, default: null },
  loading: Boolean,
  title: { type: String, default: "" },
  noPadding: Boolean,
  isDark: Boolean,
  hideProfileMenu: Boolean
});
const slots = useSlots();
const { t } = useI18n();

const sidebarOpen = ref(false);

const showTitle = computed(() => !!(slots.title || props.title));
const showBreadcrumbs = computed(() => !!props.breadcrumbs);

// Style de transitions
const overlayTransition = {
  enter: "ease-linear duration-300",
  enterFrom: "opacity-0",
  enterTo: "opacity-100",
  leave: "ease-linear duration-300",
  leaveFrom: "opacity-100",
  leaveTo: "opacity-0"
};

const closeBtnTransition = {
  enter: "ease-in-out duration-300",
  enterFrom: "opacity-0",
  enterTo: "opacity-100",
  leave: "ease-in-out duration-300",
  leaveFrom: "opacity-100",
  leaveTo: "opacity-0"
};

const slidingPanelTransition = {
  enter: "ease-in-out duration-300",
  enterFrom: "-translate-x-full motion-reduce:translate-x-0 motion-reduce:opacity-0",
  enterTo: "translate-x-0 motion-reduce:opacity-100",
  leave: "ease-in-out duration-300",
  leaveFrom: "translate-x-0 motion-reduce:opacity-100",
  leaveTo: "-translate-x-full motion-reduce:translate-x-0 motion-reduce:opacity-0"
};
</script>

<i18n>
{
	"en": {
		"account-setting": "Account settings",
		"admin-users": "User administration",
		"edit-profile": "Profile",
		"user-menu": "User account options"
	},
	"fr": {
		"account-setting": "Réglages du compte",
		"admin-users": "Gestion des utilisateurs",
		"edit-profile": "Profil",
		"user-menu": "Options du compte utilisateur"
	}
}
</i18n>

<template>
  <Menu v-slot="{ open }" as="div" class="ml-3 relative" data-test-id="profile-menu">
    <div>
      <MenuButton
        data-test-id="profile-menu-button"
        class="max-w-xs flex items-center text-sm rounded-full text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500">
        <PfIcon class="xs:mx-2 shrink-0" size="md" :icon="USER_ICON" aria-hidden="true" />
        <span v-if="userProfile && displayName" class="sr-only xs:not-sr-only xs:text-p1">{{ displayName }}</span>
        <span class="sr-only">{{ t("user-menu") }}</span>
        <PfIcon
          class="xs:mx-2 shrink-0 transition-transform ease duration-300 motion-reduce:transition-none"
          :class="open ? 'rotate-180' : 'rotate-0'"
          size="sm"
          :icon="CHEVRON_ICON"
          aria-hidden="true" />
      </MenuButton>
    </div>
    <Transition v-bind="dropdownMenuTransition">
      <MenuItems
        class="origin-top-right absolute right-0 mt-2 w-48 rounded-md shadow-lg py-1 bg-white dark:bg-grey-800 focus:outline-none">
        <ProfileMenuItem :router-path="{ name: $consts.urls.URL_PROFILE_EDIT }" :label="t('edit-profile')" />
        <ProfileMenuItem :router-path="{ name: $consts.urls.URL_ACCOUNT_SETTINGS }" :label="t('account-setting')" />
        <ProfileMenuItem v-if="managesAllUsers" :router-path="{ name: $consts.urls.URL_ADMIN_USERS }" :label="t('admin-users')" />
        <MenuItem v-if="userProfile" v-slot="{ active }">
          <LogoutBtn
            class="block rounded-none text-grey-700 dark:text-grey-200 text-sm font-normal leading-tight w-full px-3 py-2 transition-colors ease-in-out duration-200 hover:text-black focus:text-black hover:bg-primary-50 focus:bg-primary-50 dark:hover:text-white dark:focus:text-white dark:hover:bg-primary-700 dark:focus:bg-primary-700"
            :class="{ 'bg-primary-50 text-black dark:bg-primary-700 dark:text-white': active }" />
        </MenuItem>
      </MenuItems>
    </Transition>
  </Menu>
</template>

<script setup>
import { computed } from "vue";
import gql from "graphql-tag";
import { storeToRefs } from "pinia";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";

import { Menu, MenuButton, MenuItem, MenuItems } from "@headlessui/vue";
import LogoutBtn from "@/components/app/logout-btn";
import ProfileMenuItem from "@/components/app/profile-menu-item";

import { useAuthStore } from "@/lib/store/auth";
import { GLOBAL_MANAGE_ALL_USERS } from "@/lib/consts/permissions";

import CHEVRON_ICON from "@/lib/icons/chevron-down.json";
import USER_ICON from "@/lib/icons/user.json";

const { t } = useI18n();

// Manage login state
const { isLoggedIn, getGlobalPermissions } = storeToRefs(useAuthStore());

// Display profile name
const userProfile = getUserProfile();

const displayName = computed(() => {
  if (!userProfile?.value) return null;
  const profile = userProfile.value.profile;
  if (profile) {
    const { firstName, lastName } = profile;
    const fullName = `${firstName || ""} ${lastName || ""}`;

    if (fullName.trim() !== "") {
      return fullName;
    }
  }

  return userProfile.value.email;
});

const managesAllUsers = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ALL_USERS);
});

function getUserProfile() {
  const { result } = useQuery(
    gql`
      query GetUserProfile {
        me {
          id
          email
          profile {
            id
            firstName
            lastName
          }
          type
        }
      }
    `,
    null,
    () => ({
      enabled: isLoggedIn.value
    })
  );

  return useResult(result, null, (data) => data.me);
}

// Style de transition

const dropdownMenuTransition = {
  enterActiveClass: "transition ease-out duration-100",
  enterFromClass: "opacity-0 scale-95",
  enterToClass: "opacity-100 scale-100",
  leaveActiveClass: "transition ease-in duration-75",
  leaveFromClass: "opacity-100 scale-100",
  leaveToClass: "opacity-0 scale-95"
};
</script>

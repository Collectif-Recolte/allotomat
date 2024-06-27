<i18n>
{
	"en": {
		"logout": "Logout"
	},
	"fr": {
		"logout": "Déconnexion"
	}
}
</i18n>

<template>
  <PfButtonAction
    v-if="isLoggedIn"
    class="no-underline"
    btn-style="link"
    :icon="ICON_LOGOUT"
    has-icon-left
    :label="t('logout')"
    data-test-id="logout"
    @click="logout" />
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { storeToRefs } from "pinia";
import AuthenticationService from "@/lib/services/authentication";
import { useAuthStore } from "@/lib/store/auth";
import ICON_LOGOUT from "@/lib/icons/logout.json";

const { t } = useI18n();

const { isLoggedIn } = storeToRefs(useAuthStore());
const logout = function () {
  AuthenticationService.logout();
};
</script>

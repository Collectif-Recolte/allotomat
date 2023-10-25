<i18n>
{
	"en": {
		"submit": "Update",
		"general-profile-success-notification": "The profile of {name} has been successfully updated.",
    "my-profile-success-notification": "Your profile has been successfully updated.",
		"title": "User edition"
	},
	"fr": {
		"submit": "Mettre à jour",
		"general-profile-success-notification": "Le profil de {name} a bien été mis à jour.",
    "my-profile-success-notification": "Votre profil a bien été mis à jour.",
		"title": "Édition d'utilisateur"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :title="t('title')" :has-footer="false" :return-route="{ name: URL_ADMIN_USERS }">
    <AccountForm
      v-if="userProfile"
      :submit-label="t('submit')"
      :initial-values="initialFormValues"
      is-in-edition
      @submit="onSubmit"
      @closeModal="closeModal" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { URL_ADMIN_USERS } from "@/lib/consts/urls";
import { useNotificationsStore } from "@/lib/store/notifications";

import AccountForm from "@/views/admin/_Form";

const { addSuccess } = useNotificationsStore();
const { t } = useI18n();

const router = useRouter();
const route = useRoute();

const userProfileQuery = useQuery(
  gql`
    query GetUserProfile($id: ID!) {
      user(id: $id) {
        id
        email
        profile {
          id
          firstName
          lastName
        }
      }
      me {
        id
      }
    }
  `,
  {
    id: route.params.id
  }
);

const userProfile = useResult(userProfileQuery.result);

const { mutate: updateProfile } = useMutation(
  gql`
    mutation UpdateProfile($input: UpdateUserProfileInput!) {
      updateUserProfile(input: $input) {
        user {
          id
          profile {
            id
            firstName
            lastName
          }
        }
      }
    }
  `
);

const initialFormValues = computed(() => {
  if (!userProfile?.value?.user.profile) return null;
  return {
    firstName: userProfile.value.user.profile.firstName ?? "",
    lastName: userProfile.value.user.profile.lastName ?? "",
    email: userProfile.value.user.email
  };
});

async function onSubmit({ firstName, lastName }) {
  let input = { userId: userProfile.value.user.id };
  if (firstName) {
    input.firstName = { value: firstName };
  }

  if (lastName) {
    input.lastName = { value: lastName };
  }

  await updateProfile({ input });
  addSuccess(
    userProfile.value.me.id === userProfile.value.user.id
      ? t("my-profile-success-notification")
      : t("general-profile-success-notification", { name: firstName })
  );
  router.push({ name: URL_ADMIN_USERS });
}
</script>

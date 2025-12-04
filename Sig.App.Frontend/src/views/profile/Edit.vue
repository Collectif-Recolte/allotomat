<i18n>
{
	"en": {
		"first-name": "First name",
		"last-name": "Last name",
		"loading": "Update in progress...",
		"submit": "Update",
		"success-notification": "Your profile has been successfully updated.",
		"title": "Profile editing"
	},
	"fr": {
		"first-name": "Prénom",
		"last-name": "Nom",
		"loading": "Mise à jour en cours...",
		"submit": "Mettre à jour",
		"success-notification": "Votre profil a bien été mis à jour.",
		"title": "Édition de profil"
	}
}
</i18n>

<template>
  <AppShell :loading="loading" :title="t('title')">
    <div class="max-w-sm lg:w-96">
      <Form
        v-if="initialFormValues"
        v-slot="{ isSubmitting }"
        :validation-schema="validationSchema"
        :initial-values="initialFormValues"
        @submit="onSubmit">
        <PfForm has-footer :submit-label="t('submit')" :loading-label="t('loading')" :processing="isSubmitting">
          <PfFormSection>
            <Field v-slot="{ field, errors }" name="firstName">
              <PfFormInputText
                id="firstName"
                :model-value="field.value"
                :label="t('first-name')"
                :errors="errors"
                @update:modelValue="field.onChange" />
            </Field>

            <Field v-slot="{ field, errors }" name="lastName">
              <PfFormInputText
                id="lastName"
                :model-value="field.value"
                :label="t('last-name')"
                :errors="errors"
                @update:modelValue="field.onChange" />
            </Field>
          </PfFormSection>
        </PfForm>
      </Form>
    </div>
  </AppShell>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { string, object } from "yup";

import { URL_ROOT } from "@/lib/consts/urls";
import { useNotificationsStore } from "@/lib/store/notifications";
import { usePageTitle } from "@/lib/helpers/page-title";

const { addSuccess } = useNotificationsStore();
const { t } = useI18n();
const router = useRouter();

usePageTitle(t("title"));

const { result, loading } = useQuery(
  gql`
    query GetMe {
      me {
        id
        profile {
          id
          firstName
          lastName
        }
      }
    }
  `
);
const me = useResult(result);

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
  if (!me?.value?.profile) return null;
  return { firstName: me.value.profile.firstName, lastName: me.value.profile.lastName };
});

const validationSchema = computed(() =>
  object({
    firstName: string().label(t("first-name")).required(),
    lastName: string().label(t("last-name")).required()
  })
);

async function onSubmit({ firstName, lastName }) {
  let input = { userId: me.value.id };
  if (firstName) {
    input.firstName = { value: firstName };
  }

  if (lastName) {
    input.lastName = { value: lastName };
  }

  await updateProfile({ input });
  addSuccess(t("success-notification"));
  router.push({ name: URL_ROOT });
}
</script>

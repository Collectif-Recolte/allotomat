<i18n>
{
	"en": {
    "suptitle": "'Carte proximité' program",
		"title": "Card branding",
    "image-recto-label": "Image of the front of the cards",
    "image-recto-upload": "Select an image in JPG or PNG format. The dimensions should be 1087x709 pixels. Take into account that 3 mm will be trimmed on each side when cutting the cards.",
    "image-recto-preview": "The yellow outline shows the part that will be cut off when printing. Make sure any text or visuals are within the boundaries.",
    "update": "Update",
    "edit-project-card-image-success-notification": "Editing the visual of the program card {projectName} was successful."
	},
	"fr": {
    "suptitle": "Programme Carte proximité",
		"title": "Visuel de la carte",
    "image-recto-label": "Image du recto des cartes",
    "image-recto-upload": "Sélectionnez une image en format JPG ou PNG. Les dimensions doivent être de 1087x709 pixels. Prenez en compte que 3 mm vont être rognés de chaque côté lors de la coupe des cartes.",
    "image-recto-preview": "Le contour jaune montre la partie qui sera coupée lors de l’impression. Assurez-vous que tout texte ou visuel est à l’intérieur des limites.",
    "update": "Mettre à jour",
    "edit-project-card-image-success-notification": "L’édition du visuel de la carte du programme {projectName} a été un succès."
	}
}
</i18n>

<template>
  <AppShell :loading="loading">
    <template #title>
      <Title :title="t('title')" :suptitle="t('suptitle')" />
    </template>
    <div v-if="canManageCards && project" class="max-w-lg">
      <Form v-slot="{ isSubmitting }" :initial-values="initialValues" :validation-schema="validationSchema" @submit="onSubmit">
        <PfForm has-footer :submit-label="t('update')" :processing="isSubmitting">
          <PfFormSection>
            <Field v-slot="{ handleChange, field }" name="imageRectoId" :label="t('image-recto-label')">
              <UiImageUploadField
                :field="field"
                :handle-change="handleChange"
                :label="t('image-recto-label')"
                :description-upload="t('image-recto-upload')"
                :description-preview="t('image-recto-preview')">
                <template #guidelines>
                  <div class="absolute inset-3.5 border-2 border-secondary-500 rounded-[38px]"></div>
                  <div class="absolute inset-0 border-2 border-secondary-500"></div>
                </template>
              </UiImageUploadField>
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
import { storeToRefs } from "pinia";
import { object } from "yup";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useAuthStore } from "@/lib/store/auth";

import { GLOBAL_MANAGE_CARDS } from "@/lib/consts/permissions";

import Title from "@/components/app/title";

const { t } = useI18n();
usePageTitle(t("title"));
const { addSuccess } = useNotificationsStore();

const { getGlobalPermissions } = storeToRefs(useAuthStore());
const canManageCards = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_CARDS);
});

const { result, loading } = useQuery(
  gql`
    query Projects {
      projects {
        id
        name
        cardImageFileId
      }
    }
  `
);
const project = useResult(result, null, (data) => {
  initialValues.imageRectoId = data.projects[0].cardImageFileId;
  return data.projects[0];
});

const { mutate: editProject } = useMutation(
  gql`
    mutation EditProject($input: EditProjectInput!) {
      editProject(input: $input) {
        project {
          id
          cardImageFileId
        }
      }
    }
  `
);

const initialValues = {
  imageRectoId: ""
};

const validationSchema = computed(() => object({}));

async function onSubmit({ imageRectoId }) {
  await editProject({
    input: {
      projectId: project.value.id,
      cardImageFileId: { value: imageRectoId ?? "" }
    }
  });
  addSuccess(t("edit-project-card-image-success-notification", { projectName: project.value.name }));
}
</script>

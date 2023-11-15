<i18n>
{
	"en": {
		"generate-cards": "Generate new cards",
		"title": "Cards",
    "empty-list": "You haven't generated any cards yet.",
    "empty-list-organisation":"No organization is associated with the program.",
    "add-organization": "Add an organization",
    "empty-card-list": "The program has not generated any cards yet.",
    "create-gift-card": "Create a gift card",
    "available-card": "{n} available",
    "total-card-count": "{n} program cards"
	},
	"fr": {
		"generate-cards": "Générer de nouvelles cartes",
		"title": "Cartes",
    "empty-list": "Vous n'avez pas encore généré de cartes.",
    "empty-list-organisation": "Aucun organisme n'est associé au programme.",
    "add-organization": "Ajouter un organisme",
    "empty-card-list": "Le programme n'a pas encore généré de cartes.",
    "create-gift-card": "Créer une carte-cadeau",
    "available-card": "{n} disponibles",
    "total-card-count":"{n} cartes du programme"
	}
}
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loadingProjects">
      <template #title>
        <Title :title="t('title')">
          <template v-if="project" #bottom>
            <div class="flex flex-col gap-y-4 sm:flex-row sm:gap-x-4 sm:justify-between sm:items-center">
              <div class="flex flex-wrap gap-x-4">
                <h2 class="my-0">{{ t("total-card-count", project.cards.totalCount) }}</h2>
                <p class="my-1">{{ t("available-card", project.cardStats.cardsUnassigned) }}</p>
              </div>
              <div class="flex flex-wrap gap-x-4 gap-y-3">
                <PfButtonLink
                  v-if="project"
                  btn-style="outline"
                  tag="routerLink"
                  :label="t('create-gift-card')"
                  :to="showCreateGiftCardBtn" />
                <PfButtonLink
                  v-if="project"
                  tag="routerLink"
                  :label="t('generate-cards')"
                  :to="{ name: URL_CARDS_ADD, query: { projectId: project.id } }" />
              </div>
            </div>
          </template>
        </Title>
      </template>
      <Component :is="Component" v-if="showContent" />
      <!--UiEmptyPage v-else-if="organizations && organizations.length === 0">
        <UiCta
          :img-src="require('@/assets/img/organismes.jpg')"
          :description="t('empty-list-organisation')"
          :primary-btn-label="t('add-organization')"
          :primary-btn-route="addOrganizationRoute">
        </UiCta>
      </UiEmptyPage>
      <UiEmptyPage v-else-if="canManageOrganizations && organizations && project">
        <UiCta
          :img-src="require('@/assets/img/cards.jpg')"
          :description="t('empty-list')"
          :primary-btn-label="t('generate-cards')"
          :primary-btn-route="{ name: URL_CARDS_ADD, query: { projectId: project.id } }" />
      </UiEmptyPage>
      <UiEmptyPage v-else-if="!canManageOrganizations && organizations && project">
        <UiCta :img-src="require('@/assets/img/cards.jpg')" :description="t('empty-card-list')" />
      </UiEmptyPage-->
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useRoute } from "vue-router";
import { storeToRefs } from "pinia";
import { onBeforeRouteUpdate } from "vue-router";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useAuthStore } from "@/lib/store/auth";

import { URL_CARDS_ADD, URL_CARDS_ASSIGNATION, URL_CARDS_SUMMARY, URL_GIFT_CARD_ADD } from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";

import Title from "@/components/app/title";

const { getGlobalPermissions } = storeToRefs(useAuthStore());
const { t } = useI18n();

const route = useRoute();

usePageTitle(t("title"));

const canManageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

const {
  result: resultProjects,
  loading: loadingProjects,
  refetch: refetchProjects
} = useQuery(
  gql`
    query Projects {
      projects {
        id
        cardStats {
          cardsAssigned
          cardsUnassigned
        }
        cards(page: 1, limit: 30) {
          totalCount
        }
      }
    }
  `,
  null,
  {
    enabled: canManageOrganizations
  }
);

const project = useResult(resultProjects, null, (data) => {
  return data.projects[0];
});

const hasCards = computed(
  () => project.value && project.value.cardStats.cardsAssigned + project.value.cardStats.cardsUnassigned > 0
);

const showContent = computed(() => hasCards.value || route.name == URL_CARDS_ADD);

const showCreateGiftCardBtn = computed(() => {
  return project.value ? { name: URL_GIFT_CARD_ADD, query: { projectId: project.value.id } } : null;
});

onBeforeRouteUpdate((to) => {
  if (to.name === URL_CARDS_SUMMARY || to.name === URL_CARDS_ASSIGNATION) {
    refetchProjects();
  }
});
</script>

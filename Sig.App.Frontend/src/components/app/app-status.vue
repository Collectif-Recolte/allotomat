<i18n>
{
	"en": {
		"out-of-date": "A new version of the app is available.",
		"refresh": "Refresh",
		"unavailable": "We are sorry, the app is currently experiencing some issues. We are working to fix it."
	},
	"fr": {
		"out-of-date": "Une nouvelle version de l'application est disponible.",
		"refresh": "Rafraîchir",
		"unavailable": "Désolé, l'application rencontre présentement des problèmes. Nous travaillons à régler la situation."
	}
}
</i18n>

<template>
  <div v-if="isUnavailable || isOutOfDate" class="w-xl px-section max-w-full my-4 mx-auto xs:text-center">
    <p v-if="isUnavailable" class="mb-0">
      {{ t("unavailable") }}
    </p>
    <div v-else-if="isOutOfDate" class="xs:inline-flex xs:items-center xs:mx-auto xs:space-x-6">
      <p class="mb-3 xs:mb-0">
        {{ t("out-of-date") }}
      </p>
      <PfButtonAction :label="t('refresh')" data-test-id="refresh" @click="refresh" />
    </div>
  </div>
</template>

<script setup>
import { Client } from "@/lib/helpers/client";
import { computed, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const REFRESH_INTERVAL_MS = 60_000;

checkStatus();
const interval = setInterval(checkStatus, REFRESH_INTERVAL_MS);

const status = ref();
const initialVersion = ref();
const error = ref();

const isOutOfDate = computed(() => status.value && initialVersion.value !== status.value.version);
const isUnavailable = computed(() => error.value || (status.value && !status.value.ready));

onUnmounted(stop);

async function checkStatus() {
  try {
    const response = await Client.get(`/status`);

    status.value = await response.data;
    error.value = null;

    if (!initialVersion.value) {
      initialVersion.value = status.value.version;
    } else if (isOutOfDate.value) {
      stop();
    }
  } catch (err) {
    error.value = err;
  }
}

function stop() {
  clearInterval(interval);
}

function refresh() {
  window.location.reload();
}
</script>

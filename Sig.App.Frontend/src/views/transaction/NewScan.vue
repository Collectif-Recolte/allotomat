<i18n>
  {
    "en": {
      "title": "Create a transaction",
      "start-transaction": "Scan a card",
      "manually-enter-card-number": "Manually enter card number",
      "select-card-text": "Please select a cash register below to continue.",
      "no-available-cash-register": "The market has no more cash register.",
      "select-cash-register": "Save",
      "cash-register-input": "Cash Register",
      "cash-register-saved": "Cash register saved successfully!"
    },
    "fr": {
      "title": "Créer une transaction",
      "start-transaction": "Scanner une carte",
      "manually-enter-card-number": "Saisir le numéro de la carte",
      "select-card-text": "Veuillez sélectionner une caisse ci-dessous pour continuer.",
      "no-available-cash-register": "Le commerce ne possède plus de caisse.",
      "select-cash-register": "Sauvegarder",
      "cash-register-input": "Caisse",
      "cash-register-saved": "Caisse sauvegardée avec succès !"
    }
  }
</i18n>

<template>
  <div class="flex justify-center h-full w-full py-8 lg:py-16">
    <UiCta
      v-if="selectedCashRegisterId !== null"
      class="w-full max-w-sm"
      :img-src="require('@/assets/img/scan-marchand.jpg')"
      :primary-btn-label="t('start-transaction')"
      :secondary-btn-label="t('manually-enter-card-number')"
      primary-btn-is-action
      secondary-btn-is-action
      @onPrimaryBtnClick="emit('onUpdateStep', TRANSACTION_STEPS_SCAN, {})"
      @onSecondaryBtnClick="emit('onUpdateStep', TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER, {})">
      <div v-if="!loading" class="mb-6 text-left relative border border-primary-300 rounded-lg p-4 w-full">
        <div class="flex items-center justify-between gap-x-4 mb-2">
          <h3 class="text-h4 font-semibold text-primary-900 mt-2 mb-2">
            <span>{{ selectedCashRegister.name }}</span>
          </h3>
          <PfButtonAction
            :disabled="cashRegisters.length === 1"
            is-icon-only
            btn-style="outline"
            :icon="ICON_PENCIL"
            @click="editCashRegister" />
        </div>
        <ul class="mb-0">
          <li v-for="marketGroup in selectedCashRegister.marketGroups" :key="marketGroup.id" class="text-p2">
            <div>{{ marketGroup.project.name }}</div>
            <div>{{ marketGroup.name }}</div>
          </li>
        </ul>
      </div>
    </UiCta>
    <UiCta v-else class="w-full max-w-sm h-full" :img-src="require('@/assets/img/scan-marchand.jpg')">
      <div class="text-left">
        <div v-if="cashRegisterOptions.length === 0" class="text-red-500">
          <p class="text-sm">{{ t("no-available-cash-register") }}</p>
        </div>
        <div v-else>
          <p class="text-primary-900 text-h4">{{ t("select-card-text") }}</p>
          <Form v-slot="{ errors: formErrors }" :validation-schema="validationSchema" @submit="selectCashRegister">
            <PfForm has-footer :disable-submit="Object.keys(formErrors).length > 0">
              <PfFormSection>
                <Field v-slot="{ field, errors: fieldErrors }" name="selectedCashRegister">
                  <PfFormInputSelect
                    id="selectedCashRegister"
                    v-bind="field"
                    :label="t('cash-register-input')"
                    :options="cashRegisterOptions"
                    :errors="fieldErrors" />
                </Field>
              </PfFormSection>
              <template #footer>
                <div class="pt-5">
                  <div class="flex w-full justify-end">
                    <PfButtonAction class="px-8" btn-style="secondary" :label="t('select-cash-register')" type="submit" />
                  </div>
                </div>
              </template>
            </PfForm>
          </Form>
        </div>
      </div>
    </UiCta>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { defineEmits, computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { string, object } from "yup";

import { useCashRegisterStore } from "@/lib/store/cash-register";
import { useNotificationsStore } from "@/lib/store/notifications";

import { usePageTitle } from "@/lib/helpers/page-title";
import { TRANSACTION_STEPS_SCAN, TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER } from "@/lib/consts/enums";

import ICON_PENCIL from "@/lib/icons/pencil.json";

const { t } = useI18n();
usePageTitle(t("title"));

const emit = defineEmits(["onUpdateStep"]);
const { currentCashRegister, changeCashRegister } = useCashRegisterStore();
const { addSuccess } = useNotificationsStore();

const selectedCashRegisterId = ref(currentCashRegister);

const { result, loading } = useQuery(
  gql`
    query Markets {
      markets {
        id
        cashRegisters {
          id
          name
          isArchived
          marketGroups {
            id
            name
            project {
              id
              name
            }
          }
        }
      }
    }
  `
);

const cashRegisters = useResult(result, [], (data) => {
  if (data.markets[0].cashRegisters.length === 1) {
    if (!data.markets[0].cashRegisters[0].isArchived) {
      changeCashRegister(data.markets[0].cashRegisters[0].id);
      selectedCashRegisterId.value = data.markets[0].cashRegisters[0].id;
    }
  }

  return data.markets[0].cashRegisters.map((cashRegister) => ({
    id: cashRegister.id,
    name: cashRegister.name,
    isArchived: cashRegister.isArchived,
    marketGroups: cashRegister.marketGroups
  }));
});

const cashRegisterOptions = computed(() =>
  cashRegisters.value.filter((x) => !x.isArchived).map((cashRegister) => ({ value: cashRegister.id, label: cashRegister.name }))
);

const selectedCashRegister = computed(() =>
  cashRegisters.value.find((cashRegister) => cashRegister.id === selectedCashRegisterId.value)
);

const validationSchema = computed(() =>
  object({
    selectedCashRegister: string().label(t("cash-register-input")).required()
  })
);

function editCashRegister() {
  selectedCashRegisterId.value = null;
  changeCashRegister(null);
}

function selectCashRegister(event) {
  selectedCashRegisterId.value = event.selectedCashRegister;
  changeCashRegister(event.selectedCashRegister);
  addSuccess(t("cash-register-saved"));
}
</script>

<i18n>
{
  "en": {
    "back-to-menu": "Return to main menu",
    "program-label": "Program {programName}",
    "card-number": "Card #{cardProgramCardId}",
    "card-is-disabled": "The card is deactivated.",
    "expiration-date": "Expires on {date}",
    "gift-card": "Gift card",
    "make-purchase": "Make a purchase",
    "never-expired": "Never expires",
    "title": "Card balance"
  },
  "fr": {
    "back-to-menu": "Retour au menu principal",
    "program-label": "Programme {programName}",
    "card-number": "Carte #{cardProgramCardId}",
    "card-is-disabled": "La carte est désactivée.",
    "expiration-date": "Expire le {date}",
    "gift-card": "Carte-cadeau",
    "make-purchase": "Faire un achat",
    "never-expired": "N'expire jamais",
    "title": "Solde de la carte"
  }
}
</i18n>

<template>
  <div class="flex flex-col items-center justify-center min-h-[var(--kiosk-content-min-height)] px-6 xs:px-8 sm:px-12 py-12 w-full">
    <div class="bg-white rounded-4xl shadow-lg w-full max-w-2xl p-8 sm:p-10">
      <h1 class="font-bold text-d2 text-primary-700 my-0">{{ t("title") }}</h1>
      <p v-if="card" class="mt-2 text-grey-600 text-d6">
        <span>{{ t("program-label", { programName }) }}</span>
        <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
        <span> | {{ t("card-number", { cardProgramCardId: cardIdDisplay }) }}</span>
      </p>
      <p v-if="card && card.isDisabled" class="text-red-500 font-bold text-center mb-4">{{ t("card-is-disabled") }}</p>

      <p v-if="card" class="font-bold text-primary-900 text-d2 sm:text-d1 leading-none text-left mb-4">
        {{ getMoneyFormat(fund) }}
      </p>

      <ul v-if="card && productGroups.length > 0" class="flex-1 my-8 border-t border-grey-200">
        <li
          v-for="(product, index) in productGroups"
          :key="index"
          class="flex items-center justify-between gap-4 py-3 border-b border-grey-200 last:border-b-0">
          <div class="text-d7 font-bold px-3 py-1 rounded-lg" :class="getKioskCategoryClasses(product)">
            {{ getProductGroupLabel(product.label) }}
          </div>
          <div class="text-right shrink-0">
            <div class="font-bold text-h3 sm:text-h2 text-primary-900 leading-none whitespace-nowrap">
              {{ getMoneyFormat(product.fund) }}
            </div>
            <div class="text-p2 text-grey-600 leading-snug">
              <template v-if="product.expirationDate">
                {{ t("expiration-date", { date: formatDate(dateUtc(product.expirationDate), textualFormat) }) }}
              </template>
              <template v-else>
                {{ t("never-expired") }}
              </template>
            </div>
          </div>
        </li>
      </ul>

      <div class="flex flex-col xs:flex-row xs:justify-between gap-4 mt-auto">
        <PfButtonAction
          class="min-h-20 rounded-2xl text-d6"
          size="lg"
          btn-style="secondary"
          has-icon-left
          :icon="ICON_HOME"
          :label="t('back-to-menu')"
          @click="emit('finished')" />
        <PfButtonAction
          class="min-h-20 rounded-2xl text-d6"
          size="lg"
          btn-style="primary"
          has-icon-left
          :icon="ICON_SHOPPING_CART"
          :label="t('make-purchase')"
          @click="emit('startPurchase')" />
      </div>
    </div>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { computed, defineEmits, defineProps, watch } from "vue";
import { useQuery, useResult } from "@vue/apollo-composable";

import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";
import { KIOSK_ACCESS_INVALID } from "@/lib/consts/qr-code-error";
import { formatDate, dateUtc, textualFormat } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";
import { getKioskProductGroupCardClasses } from "@/lib/helpers/products-color";

import ICON_HOME from "@/lib/icons/home.json";
import ICON_SHOPPING_CART from "@/lib/icons/shopping-cart.json";

const { t } = useI18n();

const props = defineProps({
  cardId: { type: String, required: true },
  kioskToken: { type: String, required: true }
});

const emit = defineEmits(["onUpdateLoadingState", "finished", "startPurchase", "kioskAuthError"]);

const { result, loading, onError } = useQuery(
  gql`
    query KioskCardBalance($kioskToken: String!, $id: ID!) {
      kioskCard(kioskToken: $kioskToken, id: $id) {
        id
        isDisabled
        programCardId
        totalFund
        project {
          id
          name
        }
        addingFundTransactions {
          expirationDate
          availableFund
          status
          productGroup {
            id
            name
            orderOfAppearance
            color
          }
        }
        loyaltyFund {
          id
          amount
          productGroup {
            id
            name
            orderOfAppearance
            color
          }
        }
      }
    }
  `,
  () => ({ kioskToken: props.kioskToken, id: props.cardId }),
  () => ({ enabled: !!props.kioskToken && !!props.cardId })
);

onError((error) => {
  if ((error.message || "").indexOf(KIOSK_ACCESS_INVALID) !== -1) {
    emit("kioskAuthError");
  }
});

const card = useResult(result, null, (data) => data.kioskCard);

watch(loading, (value) => emit("onUpdateLoadingState", value));

const fund = computed(() => {
  if (!card.value) return 0;
  let total = card.value.totalFund;
  if (card.value.loyaltyFund) {
    total += card.value.loyaltyFund.amount;
  }
  return total;
});

const cardIdDisplay = computed(() => card.value?.programCardId ?? "");

const programName = computed(() => card.value?.project?.name ?? "");

const allFunds = computed(() => {
  if (!card.value) return [];
  const funds = card.value.addingFundTransactions ? [...card.value.addingFundTransactions] : [];
  if (card.value.loyaltyFund) {
    funds.push(card.value.loyaltyFund);
  }
  return funds;
});

const productGroups = computed(() => buildProductGroups(allFunds.value));

function buildProductGroups(funds) {
  const groups = [];
  for (const fundItem of funds) {
    const fundAmount = fundItem.availableFund ?? fundItem.amount ?? 0;
    if (
      fundAmount > 0 &&
      (fundItem.expirationDate > new Date().toISOString() || fundItem.productGroup.name === PRODUCT_GROUP_LOYALTY)
    ) {
      const existing = groups.find(
        (pg) => pg.label === fundItem.productGroup.name && pg.expirationDate === fundItem.expirationDate
      );
      if (existing) {
        existing.fund += fundItem.amount ?? fundItem.availableFund;
      } else {
        groups.push({
          color: fundItem.productGroup.color,
          label: fundItem.productGroup.name,
          fund: fundItem.amount ?? fundItem.availableFund,
          expirationDate: fundItem.expirationDate
        });
      }
    }
  }
  return groups;
}

function getProductGroupLabel(label) {
  if (label === PRODUCT_GROUP_LOYALTY) {
    return t("gift-card");
  }
  return label;
}

function getIsGiftCard(productGroupName) {
  return productGroupName === PRODUCT_GROUP_LOYALTY;
}

function getKioskCategoryClasses(product) {
  const styles = getKioskProductGroupCardClasses(product.color, getIsGiftCard(product.label), true);
  return [styles.border, styles.bg, styles.text];
}
</script>

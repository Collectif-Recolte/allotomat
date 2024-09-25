<i18n>
  {
    "en": {
      "title": "Transaction history",
      "payment-transaction": "{amount} in purchases",
      "adding-fund-transaction": "{amount} transferred to the card",
      "loyalty-adding-fund-transaction": "{amount} of gift funds added to the card",
      "refund-transaction": "{amount} refunded",
      "expire-fund-transaction": "{amount} expired",
      "no-transactions": "No transactions"
    },
    "fr": {
      "title": "Historique des transactions",
      "payment-transaction": "{amount} en achats",
      "adding-fund-transaction": "{amount} transférés sur la carte",
      "loyalty-adding-fund-transaction": "{amount} de fonds cadeaux ajoutés à la carte",
      "refund-transaction": "{amount} remboursés",
      "expire-fund-transaction": "Expiration de {amount}",
      "no-transactions": "Aucune transactions"
    }
  }
  </i18n>

<template>
  <div>
    <h1 class="font-semibold mt-4 mb-2 text-h2 xs:text-h1">{{ t("title") }}</h1>
    <ul v-if="props.transactions.items.length > 0" class="mb-0 min-w-48 sm:min-w-36">
      <li
        v-for="transaction in props.transactions.items"
        :key="transaction.id"
        class="mb-2 last:mb-0 border-b border-grey-300 pb-2 text-left">
        <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
        {{ formatDate(new Date(transaction.createdAt), textualFormat) }} - {{ getTransactionDescription(transaction) }}
      </li>
    </ul>
    <p v-else>{{ t("no-transactions") }}</p>
    <UiPagination
      v-if="props.transactions && props.transactions.totalPages > 1"
      :page="props.page"
      :total-pages="transactions.totalPages"
      @update:page="updatePage"></UiPagination>
  </div>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { defineProps, defineEmits } from "vue";

import { getMoneyFormat } from "@/lib/helpers/money";
import { formatDate, textualFormat } from "@/lib/helpers/date";

const { t } = useI18n();

const emit = defineEmits(["update:page"]);

const props = defineProps({
  page: {
    type: Number,
    required: true
  },
  transactions: {
    type: Object,
    required: true
  }
});

function getTransactionDescription(transaction) {
  var amount = getMoneyFormat(transaction.amount);
  switch (transaction.__typename) {
    case "PaymentTransactionGraphType":
      return t("payment-transaction", { amount });
    case "ManuallyAddingFundTransactionGraphType":
    case "SubscriptionAddingFundTransactionGraphType":
    case "OffPlatformAddingFundTransactionGraphType":
      return t("adding-fund-transaction", { amount });
    case "LoyaltyAddingFundTransactionGraphType":
      return t("loyalty-adding-fund-transaction", { amount });
    case "RefundTransactionGraphType":
      return t("refund-transaction", { amount });
    case "ExpireFundTransactionGraphType":
      return t("expire-fund-transaction", { amount });
  }
}

function updatePage(page) {
  emit("update:page", page);
}
</script>

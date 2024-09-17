<template>
  <ConfirmView :token-type="TOKEN_TYPE_MARKET_GROUP_MANAGER_INVITATION" />
</template>

<script setup>
import { TOKEN_TYPE_MARKET_GROUP_MANAGER_INVITATION } from "@/lib/consts/tokens";
import ConfirmView from "@/views/account/_ConfirmView";
</script>
